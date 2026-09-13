using WinTune.Sdk;

namespace WinTune.Plugins.Cleanup;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
