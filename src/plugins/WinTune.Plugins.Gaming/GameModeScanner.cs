using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Gaming;

internal sealed class GameModeScanner : IScanTask
{
    public string Name => "Modo Juego";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\GameBar");

            var gameModeEnabled = key?.GetValue("AutoGameModeEnabled");
            if (gameModeEnabled is null || (int)gameModeEnabled == 0)
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.gaming",
                    title: "Modo Juego desactivado",
                    description: "El Modo Juego de Windows optimiza CPU y GPU para juegos. Se recomienda activarlo.",
                    severity: FindingSeverity.Low,
                    metadata: new Dictionary<string, object>
                    {
                        ["registryKey"] = @"HKCU\SOFTWARE\Microsoft\GameBar",
                        ["valueName"] = "AutoGameModeEnabled",
                        ["recommendedValue"] = 1,
                    }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
