using WinTune.Sdk;

namespace WinTune.Plugins.Gaming;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
