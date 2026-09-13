using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Gaming;

internal sealed class XboxGvrScanner : IScanTask
{
    public string Name => "Xbox Game Bar / DVR";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR");

            var appCapture = key?.GetValue("AppCaptureEnabled");
            if (appCapture is int val && val == 1)
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.gaming",
                    title: "Xbox Game DVR activo",
                    description: "La captura automática de Xbox Game DVR consume CPU/GPU en segundo plano. Desactivarla puede mejorar fps.",
                    severity: FindingSeverity.Low,
                    metadata: new Dictionary<string, object>
                    {
                        ["registryKey"] = @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
                        ["valueName"] = "AppCaptureEnabled",
                        ["recommendedValue"] = 0,
                    }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
