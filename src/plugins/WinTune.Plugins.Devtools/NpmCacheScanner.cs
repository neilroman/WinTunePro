using WinTune.Sdk;

namespace WinTune.Plugins.Devtools;

internal sealed class NpmCacheScanner : IScanTask
{
    private const long MinReportBytes = 200L * 1024 * 1024;

    public string Name => "npm / yarn / pnpm cache";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var cachePaths = new Dictionary<string, string>
        {
            ["npm"] = Path.Combine(userProfile, "AppData", "Roaming", "npm-cache"),
            ["yarn"] = Path.Combine(userProfile, "AppData", "Local", "Yarn", "Cache"),
            ["pnpm"] = Path.Combine(userProfile, "AppData", "Local", "pnpm", "store"),
        };

        foreach (var (tool, path) in cachePaths)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(path)) continue;

            try
            {
                long size = 0;
                var files = new List<string>();
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try { size += new FileInfo(f).Length; files.Add(f); } catch { }
                }

                if (size >= MinReportBytes)
                {
                    findings.Add(Finding.Create(
                        moduleId: "wintune.devtools",
                        title: $"Cache de {tool}: {size / (1024.0 * 1024):F0} MB",
                        description: $"Directorio: {path}",
                        severity: FindingSeverity.Low,
                        estimatedBytesSaved: size,
                        metadata: new Dictionary<string, object>
                        {
                            ["paths"] = files,
                            ["rootPath"] = path,
                            ["tool"] = tool,
                        }));
                }
            }
            catch { }
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
