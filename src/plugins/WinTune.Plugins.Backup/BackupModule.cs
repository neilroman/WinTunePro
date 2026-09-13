using System.Management;
using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Backup;

public sealed class BackupModule : IOptimizerModule
{
    private readonly ILogger<BackupModule> _log;

    public BackupModule(ILogger<BackupModule> log) => _log = log;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.backup",
        DisplayName: "Puntos de Restauración",
        Description: "Gestiona y supervisa los puntos de restauración del sistema.",
        Category: ModuleCategory.Backup,
        Version: new Version(0, 1, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadRegistry,      CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.CreateRestorePoint, CapabilityLevel.ElevatedRequired),
    ];

    public Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"\\.\root\default",
                "SELECT * FROM SystemRestoreItem WHERE Drive = 'C:\\'");

            var items = searcher.Get()
                .Cast<ManagementObject>()
                .OrderByDescending(o => o["CreationTime"]?.ToString())
                .ToList();

            int count = items.Count;

            if (count == 0)
            {
                findings.Add(Finding.Create(
                    "wintune.backup",
                    "Sin puntos de restauración",
                    "No hay puntos de restauración en C:. El sistema no puede revertir cambios.",
                    FindingSeverity.Critical, 0,
                    new() { ["count"] = 0 }));
            }
            else
            {
                var newestStr = items[0]["CreationTime"]?.ToString();
                if (TryParseCreationTime(newestStr, out var newest))
                {
                    var daysSince = (int)(DateTimeOffset.UtcNow - newest).TotalDays;
                    if (daysSince > 7)
                    {
                        findings.Add(Finding.Create(
                            "wintune.backup",
                            "Punto de restauración desactualizado",
                            $"El último punto de restauración tiene {daysSince} días.",
                            FindingSeverity.High, 0,
                            new() { ["daysSince"] = daysSince, ["count"] = count }));
                    }
                }

                if (count > 8)
                {
                    findings.Add(Finding.Create(
                        "wintune.backup",
                        "Muchos puntos de restauración",
                        $"Hay {count} puntos de restauración ocupando espacio en disco.",
                        FindingSeverity.Low, 0,
                        new() { ["count"] = count }));
                }
            }

            foreach (var o in items) o.Dispose();
        }
        catch (ManagementException ex)
        {
            _log.LogWarning(ex, "WMI no disponible para SystemRestoreItem");
            findings.Add(Finding.Create(
                "wintune.backup", "WMI no disponible",
                "No se pudo consultar los puntos de restauración vía WMI.",
                FindingSeverity.Info));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Error al escanear puntos de restauración");
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct) =>
        Task.FromResult(ChangeSet.Empty(Metadata.Id));

    public Task<ApplyResult> ApplyAsync(ChangeSet changeSet, CancellationToken ct) =>
        Task.FromResult(new ApplyResult(
            changeSet.Id, Metadata.Id, false, [],
            [new FailedChange(changeSet.Id, "Crear punto de restauración",
                "Requiere el proceso Broker elevado — usa el botón 'Crear punto' en la página.")],
            null, DateTimeOffset.UtcNow));

    public Task RevertAsync(ApplyResult result, CancellationToken ct)
    {
        _log.LogInformation("Revert de backup no implementado");
        return Task.CompletedTask;
    }

    private static bool TryParseCreationTime(string? raw, out DateTimeOffset result)
    {
        result = default;
        if (raw is null || raw.Length < 14) return false;
        // Format: yyyyMMddHHmmss.ffffff-000
        if (DateTimeOffset.TryParseExact(
            raw[..14], "yyyyMMddHHmmss",
            null, System.Globalization.DateTimeStyles.AssumeLocal, out var dt))
        {
            result = dt;
            return true;
        }
        return false;
    }
}
