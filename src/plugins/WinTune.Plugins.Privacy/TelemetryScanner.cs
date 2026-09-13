using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Privacy;

internal sealed class TelemetryScanner : IScanTask
{
    private static readonly (string KeyPath, string Value, string Title, string Description)[] Checks =
    [
        (
            @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            "AllowTelemetry",
            "Telemetría de Windows habilitada",
            "Windows envía datos de diagnóstico a Microsoft. Nivel recomendado: 0 (Seguridad)."
        ),
        (
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
            "AllowTelemetry",
            "Recopilación de datos habilitada",
            "La recopilación de datos de diagnóstico está activa."
        ),
        (
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
            "Enabled",
            "ID publicitario activo",
            "El ID publicitario está habilitado para apps de la Tienda."
        ),
    ];

    private readonly ILogger _log;

    public TelemetryScanner(ILogger log) => _log = log;

    public string Name => "Telemetría y privacidad";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        foreach (var (keyPath, valueName, title, desc) in Checks)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath)
                             ?? Registry.CurrentUser.OpenSubKey(keyPath);
                if (key is null) continue;

                var current = key.GetValue(valueName);
                if (current is null) continue;

                bool isBad = current is int v && v > 0;
                if (isBad)
                {
                    findings.Add(Finding.Create(
                        moduleId: "wintune.privacy",
                        title: title,
                        description: desc,
                        severity: FindingSeverity.Medium,
                        metadata: new Dictionary<string, object>
                        {
                            ["registryKey"] = $@"HKLM\{keyPath}",
                            ["valueName"] = valueName,
                            ["targetValue"] = 0,
                        }));
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Error comprobando clave de privacidad {Key}", keyPath);
            }
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
