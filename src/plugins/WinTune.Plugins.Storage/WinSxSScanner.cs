using WinTune.Sdk;

namespace WinTune.Plugins.Storage;

internal sealed class WinSxSScanner : IScanTask
{
    private const long MinReportBytes = 1L * 1024 * 1024 * 1024;

    public string Name => "WinSxS / DISM";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var winSxSPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS");
            if (!Directory.Exists(winSxSPath)) return Task.FromResult<IReadOnlyList<Finding>>(findings);

            long size = 0;
            foreach (var file in Directory.EnumerateFiles(winSxSPath, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try { size += new FileInfo(file).Length; } catch { }
            }

            if (size >= MinReportBytes)
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.storage",
                    title: "WinSxS supera 1 GB",
                    description: $"El componente WinSxS ocupa {size / (1024.0 * 1024 * 1024):F1} GB. Ejecutar DISM /StartComponentCleanup puede reducirlo.",
                    severity: FindingSeverity.Medium,
                    estimatedBytesSaved: size / 4,
                    metadata: new Dictionary<string, object> { ["sizeBytes"] = size }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
