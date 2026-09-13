using WinTune.Sdk;

namespace WinTune.Plugins.Performance;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
