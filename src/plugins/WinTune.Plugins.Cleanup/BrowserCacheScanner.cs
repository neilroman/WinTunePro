using WinTune.Sdk;

namespace WinTune.Plugins.Cleanup;

internal sealed class BrowserCacheScanner : IScanTask
{
    public string Name => "Browser Caches";

    private static readonly string _local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string _roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private static readonly (string Browser, string RelPath)[] _cachePaths =
    [
        ("Chrome",       @"Google\Chrome\User Data\Default\Cache"),
        ("Chrome",       @"Google\Chrome\User Data\Default\Code Cache"),
        ("Edge",         @"Microsoft\Edge\User Data\Default\Cache"),
        ("Edge",         @"Microsoft\Edge\User Data\Default\Code Cache"),
        ("Firefox",      @"Mozilla\Firefox\Profiles"),
        ("Brave",        @"BraveSoftware\Brave-Browser\User Data\Default\Cache"),
        ("Opera GX",     @"Opera Software\Opera GX Stable\Cache"),
        ("Arc",          @"Arc\User Data\Default\Cache"),
        ("Vivaldi",      @"Vivaldi\User Data\Default\Cache"),
        ("Thorium",      @"Thorium\User Data\Default\Cache"),
    ];

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        foreach (var (browser, rel) in _cachePaths)
        {
            ct.ThrowIfCancellationRequested();
            var full = Path.Combine(_local, rel);
            if (!Directory.Exists(full)) continue;

            long size = DirSize(full);
            if (size < 1024 * 1024) continue; // skip < 1 MB

            findings.Add(Finding.Create(
                moduleId: "wintune.cleanup",
                title: $"{browser} cache",
                description: $"Cache folder: {FormatBytes(size)}",
                severity: FindingSeverity.Low,
                estimatedBytesSaved: size,
                metadata: new Dictionary<string, object> { ["paths"] = new[] { full } }
            ));
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    private static long DirSize(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            }).Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
        }
        catch { return 0; }
    }

    private static string FormatBytes(long b) => b switch
    {
        < 1024 * 1024 => $"{b / 1024.0:F0} KB",
        < 1024L * 1024 * 1024 => $"{b / (1024.0 * 1024):F1} MB",
        _ => $"{b / (1024.0 * 1024 * 1024):F2} GB"
    };
}
