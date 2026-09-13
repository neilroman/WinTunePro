using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Privacy;

public sealed class PrivacyModule : IOptimizerModule
{
    private readonly ILogger<PrivacyModule> _log;
    private readonly IScanTask[] _tasks;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.privacy",
        DisplayName: "Privacidad",
        Description: "Reduce telemetría y permisos innecesarios",
        Category: ModuleCategory.Privacy,
        Version: new Version(1, 0, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadRegistry, CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.WriteRegistry, CapabilityLevel.ElevatedRequired),
    ];

    public PrivacyModule(ILogger<PrivacyModule> log)
    {
        _log = log;
        _tasks =
        [
            new TelemetryScanner(log),
            new LocationScanner(log),
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
        var changes = findings.Select(f => new PlannedChange(
            Id: f.Id,
            Kind: ChangeKind.RegistrySet,
            Description: f.Title,
            TargetPath: f.Metadata.TryGetValue("registryKey", out var rk) ? rk?.ToString() : null,
            Payload: f.Metadata.TryGetValue("valueName", out var vn) && f.Metadata.TryGetValue("targetValue", out var tv)
                ? new Dictionary<string, object> { ["valueName"] = vn!, ["value"] = tv! }
                : new Dictionary<string, object>()))
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
                if (change.TargetPath is not null
                    && change.Payload.TryGetValue("valueName", out var vn)
                    && change.Payload.TryGetValue("value", out var val))
                {
                    var hklmPath = change.TargetPath.StartsWith(@"HKLM\")
                        ? change.TargetPath[@"HKLM\".Length..] : change.TargetPath;

                    using var key = Registry.LocalMachine.CreateSubKey(hklmPath, writable: true);
                    if (key is not null)
                    {
                        if (val is int intVal)
                            key.SetValue(vn.ToString()!, intVal, RegistryValueKind.DWord);
                        else
                            key.SetValue(vn.ToString()!, val.ToString()!, RegistryValueKind.String);
                    }
                }
                applied.Add(new AppliedChange(change.Id, change.Description, 0));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo aplicar cambio de privacidad {Id}", change.Id);
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
        _log.LogInformation("Revert de privacidad requiere punto de restauración");
        return Task.CompletedTask;
    }
}
