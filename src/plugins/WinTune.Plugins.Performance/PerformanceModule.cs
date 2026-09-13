using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Performance;

public sealed class PerformanceModule : IOptimizerModule
{
    private readonly ILogger<PerformanceModule> _log;
    private readonly IScanTask[] _tasks;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.performance",
        DisplayName: "Rendimiento",
        Description: "Optimiza inicio, energía y uso de RAM",
        Category: ModuleCategory.Performance,
        Version: new Version(1, 0, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadRegistry, CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.ManageProcesses, CapabilityLevel.UserScope),
    ];

    public PerformanceModule(ILogger<PerformanceModule> log)
    {
        _log = log;
        _tasks =
        [
            new StartupOptimizer(log),
            new SleepModeScanner(log),
            new RamOptimizer(log),
        ];
    }

    public async Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        int i = 0;
        foreach (var task in _tasks)
        {
            ct.ThrowIfCancellationRequested();
            var partial = await task.ScanAsync(ct);
            findings.AddRange(partial);
            i++;
            ctx.Progress?.Report(new ScanProgress(i * 100 / _tasks.Length, task.Name));
        }
        return findings;
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct)
    {
        var changes = findings
            .Where(f => f.Severity >= FindingSeverity.Medium)
            .Select(f =>
            {
                f.Metadata.TryGetValue("registryKey", out var regKey);
                f.Metadata.TryGetValue("valueName", out var valueName);
                return new PlannedChange(
                    Id: f.Id,
                    Kind: ChangeKind.RegistryDelete,
                    Description: f.Title,
                    TargetPath: regKey?.ToString(),
                    Payload: new Dictionary<string, object>
                    {
                        ["valueName"] = valueName?.ToString() ?? string.Empty,
                    });
            })
            .ToList();

        return Task.FromResult(new ChangeSet(
            Id: Guid.NewGuid().ToString(),
            ModuleId: Metadata.Id,
            Changes: changes,
            TotalBytesSaved: 0,
            CreatedAt: DateTimeOffset.UtcNow));
    }

    public async Task<ApplyResult> ApplyAsync(ChangeSet changeSet, CancellationToken ct)
    {
        var applied = new List<AppliedChange>();
        var failed = new List<FailedChange>();

        foreach (var change in changeSet.Changes)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (change.Kind == ChangeKind.RegistryDelete && change.TargetPath is not null)
                {
                    var valueName = change.Payload.TryGetValue("valueName", out var v)
                        ? v?.ToString() : null;

                    var hklmPath = change.TargetPath.StartsWith(@"HKLM\")
                        ? change.TargetPath[@"HKLM\".Length..] : change.TargetPath;

                    using var key = Registry.LocalMachine.OpenSubKey(hklmPath, writable: true);
                    if (valueName is not null)
                        key?.DeleteValue(valueName, throwOnMissingValue: false);
                }
                applied.Add(new AppliedChange(change.Id, change.Description, 0));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo aplicar cambio {Id}", change.Id);
                failed.Add(new FailedChange(change.Id, change.Description, ex.Message));
            }
        }

        return await Task.FromResult(new ApplyResult(
            ChangeSetId: changeSet.Id,
            ModuleId: Metadata.Id,
            Success: failed.Count == 0,
            Applied: applied,
            Failed: failed,
            ReversalToken: null,
            AppliedAt: DateTimeOffset.UtcNow));
    }

    public Task RevertAsync(ApplyResult result, CancellationToken ct)
    {
        _log.LogInformation("Revert no implementado para módulo de rendimiento");
        return Task.CompletedTask;
    }
}
