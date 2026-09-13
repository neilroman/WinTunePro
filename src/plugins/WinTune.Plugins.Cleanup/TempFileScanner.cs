using WinTune.Sdk;

namespace WinTune.Plugins.Cleanup;

internal sealed class TempFileScanner : IScanTask
{
    public string Name => "Temp Files";

    private static readonly string[] _tempDirs =
    [
        Path.GetTempPath(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp"),
    ];

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        foreach (var dir in _tempDirs.Distinct())
        {
            if (!Directory.Exists(dir)) continue;
            ct.ThrowIfCancellationRequested();

            var files = SafeEnumFiles(dir);
            if (files.Count == 0) continue;

            long totalBytes = files.Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });

            findings.Add(Finding.Create(
                moduleId: "wintune.cleanup",
                title: $"Temp files in {Path.GetFileName(dir.TrimEnd('\\', '/'))}",
                description: $"{files.Count:N0} temporary files ({FormatBytes(totalBytes)})",
                severity: FindingSeverity.Low,
                estimatedBytesSaved: totalBytes,
                metadata: new Dictionary<string, object> { ["paths"] = files }
            ));
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    private static List<string> SafeEnumFiles(string dir)
    {
        var result = new List<string>();
        try
        {
            result.AddRange(Directory.EnumerateFiles(dir, "*", new EnumerationOptions
            {
                RecurseSubdirectories = false,
                IgnoreInaccessible = true,
            }));
        }
        catch { /* inaccessible dir */ }
        return result;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
