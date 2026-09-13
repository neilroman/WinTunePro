using WinTune.Sdk;

namespace WinTune.Plugins.Storage;

internal interface IScanTask
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct);
}
