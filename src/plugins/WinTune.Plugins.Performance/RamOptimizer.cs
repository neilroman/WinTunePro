using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Performance;

internal sealed class RamOptimizer : IScanTask
{
    private const long HighRamThresholdBytes = 200L * 1024 * 1024;
    private readonly ILogger _log;

    public RamOptimizer(ILogger log) => _log = log;

    public string Name => "Procesos con alto consumo de RAM";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var processes = System.Diagnostics.Process.GetProcesses();
            var highRam = processes
                .Where(p => TryGetWorkingSet(p) > HighRamThresholdBytes)
                .OrderByDescending(TryGetWorkingSet)
                .Take(5)
                .ToList();

            foreach (var proc in highRam)
            {
                ct.ThrowIfCancellationRequested();
                long ws = TryGetWorkingSet(proc);
                findings.Add(Finding.Create(
                    moduleId: "wintune.performance",
                    title: $"Proceso de alto consumo: {proc.ProcessName}",
                    description: $"Usando {ws / (1024 * 1024)} MB de RAM (PID {proc.Id})",
                    severity: FindingSeverity.Low,
                    metadata: new Dictionary<string, object>
                    {
                        ["processName"] = proc.ProcessName,
                        ["pid"] = proc.Id,
                    }));
            }

            foreach (var proc in processes) proc.Dispose();
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Error analizando procesos RAM");
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    private static long TryGetWorkingSet(System.Diagnostics.Process p)
    {
        try { return p.WorkingSet64; }
        catch { return 0; }
    }
}
