using WinTune.Sdk;

namespace WinTune.Plugins.Devtools;

internal sealed class VsArtifactsScanner : IScanTask
{
    private const long MinReportBytes = 500L * 1024 * 1024;

    public string Name => "Artefactos de Visual Studio / .NET";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var roots = new[]
        {
            Path.Combine(userProfile, "AppData", "Local", "Temp", "NuGetScratch"),
            Path.Combine(userProfile, ".nuget", "packages"),
            Path.Combine(userProfile, "AppData", "Local", "Microsoft", "VisualStudio"),
        };

        foreach (var root in roots)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(root)) continue;

            try
            {
                long size = 0;
                var files = new List<string>();
                foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try { size += new FileInfo(f).Length; files.Add(f); } catch { }
                }

                if (size >= MinReportBytes)
                {
                    findings.Add(Finding.Create(
                        moduleId: "wintune.devtools",
                        title: $"Artefactos VS/NuGet: {size / (1024.0 * 1024):F0} MB",
                        description: $"Directorio: {root}",
                        severity: FindingSeverity.Info,
                        estimatedBytesSaved: 0,
                        metadata: new Dictionary<string, object>
                        {
                            ["rootPath"] = root,
                            ["sizeBytes"] = size,
                        }));
                }
            }
            catch { }
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
