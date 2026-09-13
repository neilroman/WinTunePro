using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Performance;

internal sealed class StartupOptimizer : IScanTask
{
    private static readonly string[] StartupKeys =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
    ];

    private readonly ILogger _log;

    public StartupOptimizer(ILogger log) => _log = log;

    public string Name => "Entradas de inicio huérfanas";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        foreach (var keyPath in StartupKeys)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key is null) continue;

                foreach (var name in key.GetValueNames())
                {
                    var exePath = key.GetValue(name)?.ToString() ?? string.Empty;
                    var clean = exePath.Trim('"').Split(' ')[0];

                    if (!File.Exists(clean))
                    {
                        findings.Add(Finding.Create(
                            moduleId: "wintune.performance",
                            title: $"Entrada de inicio huérfana: {name}",
                            description: $"El ejecutable no existe: {clean}",
                            severity: FindingSeverity.Medium,
                            metadata: new Dictionary<string, object>
                            {
                                ["registryKey"] = $@"HKLM\{keyPath}",
                                ["valueName"] = name,
                            }));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Error leyendo clave de inicio {Key}", keyPath);
            }
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
