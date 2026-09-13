namespace WinTune.Sdk;

public sealed class ScanContext
{
    public bool DeepScan { get; init; } = false;
    public IProgress<ScanProgress>? Progress { get; init; }
    public IReadOnlyDictionary<string, object> Options { get; init; } = new Dictionary<string, object>();
}

public sealed record ScanProgress(int PercentComplete, string CurrentItem);
