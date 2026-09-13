using WinTune.Sdk;

namespace WinTune.Plugins.Storage;

internal sealed class LargeFileScanner : IScanTask
{
    private const long ThresholdBytes = 500L * 1024 * 1024;

    public string Name => "Archivos grandes";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var searchDirs = new[]
        {
            Path.Combine(userProfile, "Downloads"),
            Path.Combine(userProfile, "Documents"),
            Path.Combine(userProfile, "Desktop"),
        };

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;
            ct.ThrowIfCancellationRequested();

            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Length >= ThresholdBytes)
                        {
                            findings.Add(Finding.Create(
                                moduleId: "wintune.storage",
                                title: $"Archivo grande: {info.Name}",
                                description: $"{FormatBytes(info.Length)} en {dir}",
                                severity: FindingSeverity.Info,
                                estimatedBytesSaved: 0,
                                metadata: new Dictionary<string, object>
                                {
                                    ["path"] = file,
                                    ["sizeBytes"] = info.Length,
                                }));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    private static string FormatBytes(long b) => b switch
    {
        < 1024L * 1024 * 1024 => $"{b / (1024.0 * 1024):F0} MB",
        _ => $"{b / (1024.0 * 1024 * 1024):F2} GB"
    };
}
