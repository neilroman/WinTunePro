using WinTune.Sdk;

namespace WinTune.Plugins.Cleanup;

internal sealed class WindowsLogScanner : IScanTask
{
    public string Name => "Windows Logs";

    private static readonly string[] _logDirs =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\LogFiles"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Microsoft\Windows\WER\ReportArchive"),
    ];

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        foreach (var dir in _logDirs)
        {
            if (!Directory.Exists(dir)) continue;
            ct.ThrowIfCancellationRequested();

            var oldLogs = Directory.EnumerateFiles(dir, "*.*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            })
            .Where(f =>
            {
                try { return File.GetLastWriteTime(f) < DateTime.Now.AddDays(-30); }
                catch { return false; }
            })
            .ToList();

            if (oldLogs.Count == 0) continue;

            long size = oldLogs.Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });

            findings.Add(Finding.Create(
                moduleId: "wintune.cleanup",
                title: $"Old logs in {Path.GetFileName(dir)}",
                description: $"{oldLogs.Count} log files older than 30 days ({size / (1024.0 * 1024):F1} MB)",
                severity: FindingSeverity.Low,
                estimatedBytesSaved: size,
                metadata: new Dictionary<string, object> { ["paths"] = oldLogs }
            ));
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
