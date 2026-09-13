using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Gaming;

public sealed class GamingModule : IOptimizerModule
{
    private readonly ILogger<GamingModule> _log;
    private readonly IScanTask[] _tasks;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.gaming",
        DisplayName: "Modo Gaming",
        Description: "Optimiza Windows para juegos: Game Mode, plan de energía, DVR, HPET.",
        Category: ModuleCategory.Gaming,
        Version: new Version(1, 0, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadRegistry, CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.WriteRegistry, CapabilityLevel.UserScope),
    ];

    public GamingModule(ILogger<GamingModule> log)
    {
        _log = log;
        _tasks =
        [
            new GameModeScanner(),
            new PowerPlanScanner(),
            new XboxGvrScanner(),
            new HpetScanner(),
        ];
    }

    public async Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        int i = 0;
        foreach (var task in _tasks)
        {
            ct.ThrowIfCancellationRequested();
            findings.AddRange(await task.ScanAsync(ct));
            ctx.Progress?.Report(new ScanProgress(++i * 100 / _tasks.Length, task.Name));
        }
        return findings;
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct)
    {
        var changes = findings
            .Where(f => f.Metadata.ContainsKey("registryKey"))
            .Select(f => new PlannedChange(
                Id: Guid.NewGuid().ToString(),
                Kind: ChangeKind.RegistrySet,
                Description: f.Title,
                TargetPath: f.Metadata["registryKey"].ToString(),
                Payload: new Dictionary<string, object>
                {
                    ["valueName"] = f.Metadata.TryGetValue("valueName", out var vn) ? vn : string.Empty,
                    ["value"] = f.Metadata.TryGetValue("recommendedValue", out var rv) ? rv : 1,
                    ["findingId"] = f.Id,
                }))
            .ToList();

        return Task.FromResult(new ChangeSet(
            Guid.NewGuid().ToString(), Metadata.Id, changes, 0, DateTimeOffset.UtcNow));
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
                if (change.Kind == ChangeKind.RegistrySet && change.TargetPath is not null)
                {
                    var valueName = change.Payload.TryGetValue("valueName", out var vn) ? vn?.ToString() : null;
                    var value = change.Payload.TryGetValue("value", out var v) ? v : 1;

                    var (hive, subKey) = SplitRegistryPath(change.TargetPath);
                    using var key = hive.CreateSubKey(subKey, writable: true);
                    if (valueName is not null && key is not null)
                        key.SetValue(valueName, value, Microsoft.Win32.RegistryValueKind.DWord);
                }
                applied.Add(new AppliedChange(change.Id, change.Description, 0));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo aplicar {Id}", change.Id);
                failed.Add(new FailedChange(change.Id, change.Description, ex.Message));
            }
        }

        return await Task.FromResult(new ApplyResult(
            changeSet.Id, Metadata.Id, failed.Count == 0, applied, failed, null, DateTimeOffset.UtcNow));
    }

    public Task RevertAsync(ApplyResult result, CancellationToken ct) => Task.CompletedTask;

    private static (Microsoft.Win32.RegistryKey hive, string subKey) SplitRegistryPath(string path)
    {
        if (path.StartsWith(@"HKCU\"))
            return (Microsoft.Win32.Registry.CurrentUser, path[@"HKCU\".Length..]);
        if (path.StartsWith(@"HKLM\"))
            return (Microsoft.Win32.Registry.LocalMachine, path[@"HKLM\".Length..]);
        return (Microsoft.Win32.Registry.CurrentUser, path);
    }
}
