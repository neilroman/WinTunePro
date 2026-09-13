using WinTune.Sdk;

namespace WinTune.Plugins.Gaming;

internal sealed class HpetScanner : IScanTask
{
    public string Name => "Temporizador HPET";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var bcdedit = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "bcdedit",
                Arguments = "/enum {current}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (bcdedit is not null)
            {
                var output = bcdedit.StandardOutput.ReadToEnd();
                bcdedit.WaitForExit(3000);

                if (output.Contains("useplatformclock") && output.Contains("Yes"))
                {
                    findings.Add(Finding.Create(
                        moduleId: "wintune.gaming",
                        title: "HPET (reloj de plataforma) activado vía BCD",
                        description: "useplatformclock=Yes puede aumentar la latencia del temporizador en juegos.",
                        severity: FindingSeverity.Info,
                        metadata: new Dictionary<string, object> { ["source"] = "bcdedit" }));
                }
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
