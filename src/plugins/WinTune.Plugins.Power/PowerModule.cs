using Microsoft.Extensions.Logging;
using WinTune.Interop;
using WinTune.Sdk;

namespace WinTune.Plugins.Power;

public sealed class PowerModule : IOptimizerModule
{
    private readonly ILogger<PowerModule> _log;

    private static readonly Guid Balanced        = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid HighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    private static readonly Guid PowerSaver      = new("a1841308-3541-4fab-bc81-f71556f20b4a");

    public PowerModule(ILogger<PowerModule> log) => _log = log;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.power",
        DisplayName: "Energía",
        Description: "Optimiza el plan de energía según el modo de uso.",
        Category: ModuleCategory.Power,
        Version: new Version(0, 1, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadRegistry,  CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.WriteRegistry, CapabilityLevel.ElevatedRequired),
    ];

    public Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        try
        {
            var active   = PowerHelper.GetActivePowerScheme();
            var onBattery = PowerHelper.IsOnBattery();

            if (active is null)
            {
                findings.Add(Finding.Create(
                    "wintune.power", "Plan de energía desconocido",
                    "No se pudo leer el plan de energía activo.",
                    FindingSeverity.Low));
            }
            else if (onBattery && active.Value == HighPerformance)
            {
                findings.Add(Finding.Create(
                    "wintune.power", "Plan ineficiente en batería",
                    "Alto Rendimiento consume batería rápidamente. Se recomienda Equilibrado.",
                    FindingSeverity.High, 0,
                    new() { ["targetSchemeGuid"] = Balanced.ToString(),
                             ["currentSchemeGuid"] = active.Value.ToString() }));
            }
            else if (!onBattery && active.Value == PowerSaver)
            {
                findings.Add(Finding.Create(
                    "wintune.power", "Rendimiento limitado en CA",
                    "El plan Ahorro de Energía limita el rendimiento. Se recomienda Equilibrado.",
                    FindingSeverity.Medium, 0,
                    new() { ["targetSchemeGuid"] = Balanced.ToString(),
                             ["currentSchemeGuid"] = active.Value.ToString() }));
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Error al leer el plan de energía");
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct)
    {
        var changes = findings
            .Where(f => f.Metadata.ContainsKey("targetSchemeGuid"))
            .Select(f => new PlannedChange(
                Id: f.Id,
                Kind: ChangeKind.ConfigChange,
                Description: f.Title,
                TargetPath: "power://scheme",
                Payload: new Dictionary<string, object>
                {
                    ["targetSchemeGuid"]  = f.Metadata["targetSchemeGuid"],
                    ["currentSchemeGuid"] = f.Metadata.GetValueOrDefault("currentSchemeGuid", ""),
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
            if (change.Kind != ChangeKind.ConfigChange) continue;
            try
            {
                var guidStr = change.Payload.TryGetValue("targetSchemeGuid", out var v)
                    ? v?.ToString() : null;
                if (guidStr is null || !Guid.TryParse(guidStr, out var target))
                {
                    failed.Add(new(change.Id, change.Description, "GUID de plan no válido"));
                    continue;
                }
                PowerHelper.SetActivePowerScheme(target);
                applied.Add(new(change.Id, change.Description, 0));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo cambiar el plan de energía");
                failed.Add(new(change.Id, change.Description, ex.Message));
            }
        }

        return Task.FromResult(new ApplyResult(
            changeSet.Id, Metadata.Id, failed.Count == 0,
            applied, failed, null, DateTimeOffset.UtcNow));
    }

    public Task RevertAsync(ApplyResult result, CancellationToken ct)
    {
        _log.LogInformation("Revert del plan de energía no implementado");
        return Task.CompletedTask;
    }
}
