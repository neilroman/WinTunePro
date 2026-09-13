using WinTune.Sdk;

namespace WinTune.Plugins.Network;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
