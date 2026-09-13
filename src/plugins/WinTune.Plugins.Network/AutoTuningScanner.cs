using WinTune.Sdk;

namespace WinTune.Plugins.Network;

internal sealed class AutoTuningScanner : IScanTask
{
    public string Name => "TCP Auto-Tuning";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "interface tcp show global",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return Task.FromResult<IReadOnlyList<Finding>>(findings);

            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);

            if (output.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.network",
                    title: "TCP Auto-Tuning desactivado",
                    description: "La autoconfiguración del buffer TCP está desactivada. Puede reducir el rendimiento en conexiones de alta latencia.",
                    severity: FindingSeverity.Low,
                    metadata: new Dictionary<string, object> { ["source"] = "netsh" }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
