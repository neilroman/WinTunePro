namespace WinTune.App.Services;

public sealed record AppSettings
{
    public string AppTheme { get; init; } = "System";
    public bool NotificationsEnabled { get; init; } = true;
    public bool AutoScanEnabled { get; init; } = false;
    public List<string> ExcludedPaths { get; init; } = new();
}
