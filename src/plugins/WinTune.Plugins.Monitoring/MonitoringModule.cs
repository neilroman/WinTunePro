using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Monitoring;

public sealed class MonitoringModule : IOptimizerModule
{
    private const long HighRamThresholdBytes = 500L * 1024 * 1024; // 500 MB
    private readonly ILogger<MonitoringModule> _log;

    public MonitoringModule(ILogger<MonitoringModule> log) => _log = log;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.monitoring",
        DisplayName: "Monitoreo",
        Description: "Detecta procesos con alto consumo de CPU o RAM.",
        Category: ModuleCategory.Monitoring,
        Version: new Version(0, 1, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ManageProcesses, CapabilityLevel.ReadOnly),
    ];

    public Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        try
        {
            var processes = Process.GetProcesses()
                .Where(p =>
                {
                    try { return p.WorkingSet64 > HighRamThresholdBytes; }
                    catch { return false; }
                })
                .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
                .Take(10)
                .ToList();

            foreach (var p in processes)
            {
                try
                {
                    long mb = p.WorkingSet64 / (1024 * 1024);
                    findings.Add(Finding.Create(
                        "wintune.monitoring",
                        $"Proceso {p.ProcessName} usa {mb} MB",
                        $"El proceso {p.ProcessName} (PID {p.Id}) tiene alto consumo de RAM.",
                        FindingSeverity.Low, 0,
                        new()
                        {
                            ["pid"]           = p.Id,
                            ["processName"]   = p.ProcessName,
                            ["workingSetMb"]  = (int)mb,
                        }));
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Error leyendo proceso {Pid}", p.Id);
                }
                finally
                {
                    p.Dispose();
                }
            }

            foreach (var p in Process.GetProcesses().Except(processes))
                p.Dispose();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Error al enumerar procesos");
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct)
    {
        var changes = findings.Select(f => new PlannedChange(
            Id: f.Id,
            Kind: ChangeKind.KillProcess,
            Description: $"Terminar {f.Metadata.GetValueOrDefault("processName", "proceso")}",
            TargetPath: f.Metadata.GetValueOrDefault("processName", "")?.ToString(),
            Payload: new Dictionary<string, object>
            {
                ["pid"] = f.Metadata.GetValueOrDefault("pid", 0)!,
            }))
            .ToList();

        return Task.FromResult(new ChangeSet(
            Guid.NewGuid().ToString(), Metadata.Id, changes, 0, DateTimeOffset.UtcNow));
    }

    public Task<ApplyResult> ApplyAsync(ChangeSet changeSet, CancellationToken ct)
    {
        var applied = new List<AppliedChange>();
        var failed  = new List<FailedChange>();

        foreach (var change in changeSet.Changes)
        {
            if (change.Kind != ChangeKind.KillProcess) continue;
            try
            {
                if (!change.Payload.TryGetValue("pid", out var pidObj) ||
                    !int.TryParse(pidObj?.ToString(), out var pid))
                {
                    failed.Add(new(change.Id, change.Description, "PID no válido"));
                    continue;
                }
                Process.GetProcessById(pid).Kill();
                applied.Add(new(change.Id, change.Description, 0));
            }
            catch (ArgumentException)
            {
                applied.Add(new(change.Id, change.Description, 0)); // already gone
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo terminar proceso");
                failed.Add(new(change.Id, change.Description, ex.Message));
            }
        }

        return Task.FromResult(new ApplyResult(
            changeSet.Id, Metadata.Id, failed.Count == 0,
            applied, failed, null, DateTimeOffset.UtcNow));
    }

    public Task RevertAsync(ApplyResult result, CancellationToken ct)
    {
        _log.LogInformation("No se pueden revertir procesos terminados");
        return Task.CompletedTask;
    }
}
