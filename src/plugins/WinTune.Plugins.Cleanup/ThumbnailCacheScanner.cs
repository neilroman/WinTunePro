using WinTune.Sdk;

namespace WinTune.Plugins.Cleanup;

internal sealed class ThumbnailCacheScanner : IScanTask
{
    public string Name => "Thumbnail Cache";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        var thumbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Windows\Explorer");

        if (!Directory.Exists(thumbDir))
            return Task.FromResult<IReadOnlyList<Finding>>(findings);

        var files = Directory.GetFiles(thumbDir, "thumbcache_*.db")
            .Concat(Directory.GetFiles(thumbDir, "iconcache_*.db"))
            .ToList();

        if (files.Count == 0)
            return Task.FromResult<IReadOnlyList<Finding>>(findings);

        long total = files.Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });

        findings.Add(Finding.Create(
            moduleId: "wintune.cleanup",
            title: "Thumbnail & icon cache",
            description: $"{files.Count} cache files ({total / (1024.0 * 1024):F1} MB)",
            severity: FindingSeverity.Low,
            estimatedBytesSaved: total,
            metadata: new Dictionary<string, object> { ["paths"] = files }
        ));

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
