using WinTune.Sdk;

namespace WinTune.Plugins.Storage;

internal sealed class RecycleBinScanner : IScanTask
{
    private const long MinReportBytes = 100L * 1024 * 1024;

    public string Name => "Papelera de reciclaje";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            long totalSize = 0;
            var paths = new List<string>();

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                ct.ThrowIfCancellationRequested();
                var recyclePath = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
                if (!Directory.Exists(recyclePath)) continue;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(recyclePath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            totalSize += info.Length;
                            paths.Add(file);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            if (totalSize >= MinReportBytes)
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.storage",
                    title: "Papelera de reciclaje con contenido",
                    description: $"{FormatBytes(totalSize)} ocupados en la Papelera. Vaciarla liberará espacio.",
                    severity: FindingSeverity.Low,
                    estimatedBytesSaved: totalSize,
                    metadata: new Dictionary<string, object>
                    {
                        ["paths"] = paths,
                        ["totalBytes"] = totalSize,
                    }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    private static string FormatBytes(long b) => b switch
    {
        < 1024L * 1024 * 1024 => $"{b / (1024.0 * 1024):F0} MB",
        _ => $"{b / (1024.0 * 1024 * 1024):F2} GB"
    };
}
