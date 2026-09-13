using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Privacy;

internal sealed class LocationScanner : IScanTask
{
    private readonly ILogger _log;

    public LocationScanner(ILogger log) => _log = log;

    public string Name => "Permisos de ubicación";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            var value = key?.GetValue("Value")?.ToString();

            if (string.Equals(value, "Allow", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.privacy",
                    title: "Ubicación global habilitada",
                    description: "Cualquier app puede solicitar tu ubicación. Considera deshabilitarlo si no lo necesitas.",
                    severity: FindingSeverity.Low,
                    metadata: new Dictionary<string, object>
                    {
                        ["registryKey"] = $@"HKLM\{keyPath}",
                        ["valueName"] = "Value",
                        ["targetValue"] = "Deny",
                    }));
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Error comprobando permisos de ubicación");
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
