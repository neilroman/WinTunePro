using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Network;

public sealed class NetworkModule : IOptimizerModule
{
    private readonly ILogger<NetworkModule> _log;
    private readonly IScanTask[] _tasks;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.network",
        DisplayName: "Red",
        Description: "Analiza DNS, adaptadores inactivos y configuración TCP.",
        Category: ModuleCategory.Network,
        Version: new Version(1, 0, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadNetworkConfig, CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.WriteNetworkConfig, CapabilityLevel.ElevatedRequired),
    ];

    public NetworkModule(ILogger<NetworkModule> log)
    {
        _log = log;
        _tasks =
        [
            new DnsScanner(),
            new NetworkAdapterScanner(),
            new AutoTuningScanner(),
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

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct) =>
        Task.FromResult(ChangeSet.Empty(Metadata.Id));

    public Task<ApplyResult> ApplyAsync(ChangeSet changeSet, CancellationToken ct) =>
        Task.FromResult(new ApplyResult(
            changeSet.Id, Metadata.Id, true, [], [], null, DateTimeOffset.UtcNow));

    public Task RevertAsync(ApplyResult result, CancellationToken ct) => Task.CompletedTask;
}
