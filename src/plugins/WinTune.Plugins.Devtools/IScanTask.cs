using WinTune.Sdk;

namespace WinTune.Plugins.Devtools;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
