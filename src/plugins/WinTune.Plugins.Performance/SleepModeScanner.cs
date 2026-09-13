using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Performance;

internal sealed class SleepModeScanner : IScanTask
{
    private readonly ILogger _log;

    public SleepModeScanner(ILogger log) => _log = log;

    public string Name => "Configuración de energía";

    public async Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var result = await RunPowerCfgAsync("/query SCHEME_CURRENT SUB_SLEEP STANDBYIDLE", ct);
            if (result.Contains("0x00000000"))
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.performance",
                    title: "El PC nunca entra en suspensión",
                    description: "El tiempo de suspensión está en 0 (desactivado). Aumentarlo ahorra energía.",
                    severity: FindingSeverity.Low));
            }

            var hibResult = await RunPowerCfgAsync("/a", ct);
            if (hibResult.Contains("not available") || !hibResult.ToLower().Contains("hibernat"))
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.performance",
                    title: "Hibernación desactivada",
                    description: "Activar la hibernación permite encendidos rápidos y ahorra energía en laptops.",
                    severity: FindingSeverity.Low));
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Error escaneando configuración de energía");
        }

        return findings;
    }

    private static async Task<string> RunPowerCfgAsync(string args, CancellationToken ct)
    {
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        return output;
    }
}
