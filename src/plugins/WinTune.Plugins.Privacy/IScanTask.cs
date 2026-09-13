using WinTune.Sdk;

namespace WinTune.Plugins.Privacy;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
