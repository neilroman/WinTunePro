using WinTune.Sdk;

namespace WinTune.Plugins.Devtools;

internal sealed class DockerScanner : IScanTask
{
    public string Name => "Docker / contenedores";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        var dockerDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DockerDesktop", "vm-data");

        if (!Directory.Exists(dockerDataRoot))
        {
            dockerDataRoot = @"C:\ProgramData\Docker";
        }

        if (!Directory.Exists(dockerDataRoot)) return Task.FromResult<IReadOnlyList<Finding>>(findings);

        try
        {
            long size = 0;
            foreach (var f in Directory.EnumerateFiles(dockerDataRoot, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try { size += new FileInfo(f).Length; } catch { }
            }

            if (size > 1L * 1024 * 1024 * 1024)
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.devtools",
                    title: $"Docker ocupa {size / (1024.0 * 1024 * 1024):F1} GB",
                    description: "Ejecutar 'docker system prune' puede liberar espacio de imágenes y contenedores huérfanos.",
                    severity: FindingSeverity.Info,
                    estimatedBytesSaved: size / 3,
                    metadata: new Dictionary<string, object> { ["dataRoot"] = dockerDataRoot, ["sizeBytes"] = size }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
