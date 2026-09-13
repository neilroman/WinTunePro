using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Gaming;

internal sealed class PowerPlanScanner : IScanTask
{
    private static readonly Guid HighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    private static readonly Guid UltimatePerformance = new("e9a42b02-d5df-448d-aa00-03f14749eb61");
    private static readonly Guid BalancedPlan = new("381b4222-f694-41f0-9685-ff5bb260df2e");

    public string Name => "Plan de energía";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes");

            var activeScheme = key?.GetValue("ActivePowerScheme")?.ToString();
            if (activeScheme is not null)
            {
                var activePlan = Guid.TryParse(activeScheme, out var g) ? g : Guid.Empty;
                if (activePlan != HighPerformance && activePlan != UltimatePerformance)
                {
                    findings.Add(Finding.Create(
                        moduleId: "wintune.gaming",
                        title: "Plan de energía no óptimo para juegos",
                        description: $"El plan activo ({activeScheme}) limita el rendimiento. Se recomienda Alto Rendimiento o Máximo Rendimiento.",
                        severity: FindingSeverity.Medium,
                        metadata: new Dictionary<string, object>
                        {
                            ["activePlan"] = activeScheme,
                            ["recommendedPlan"] = HighPerformance.ToString(),
                        }));
                }
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
