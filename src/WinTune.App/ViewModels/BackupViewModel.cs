using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class BackupViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private string _status = "Listo para escanear";
    [ObservableProperty] private IReadOnlyList<Finding> _findings = [];

    public BackupViewModel(ModuleOrchestrator orchestrator, BrokerClient broker)
    {
        _orchestrator = orchestrator;
        _broker = broker;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Status = "Verificando puntos de restauración…";

        try
        {
            var module = _orchestrator.Modules.FirstOrDefault(m => m.Metadata.Id == "wintune.backup");
            if (module is null) { Status = "Módulo no encontrado"; return; }

            Findings = await module.ScanAsync(new ScanContext(), ct);
            Status = Findings.Count > 0
                ? $"{Findings.Count} observaciones sobre la copia de seguridad"
                : "Estado de copia de seguridad correcto";
        }
        catch (OperationCanceledException) { Status = "Cancelado"; }
        finally { IsScanning = false; }
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateRestorePointAsync()
    {
        IsCreating = true;
        Status = "Creando punto de restauración del sistema…";

        try
        {
            await _broker.CreateRestorePointAsync();
            Status = "Punto de restauración creado correctamente";
        }
        catch (Exception ex)
        {
            Status = $"Error al crear punto de restauración: {ex.Message}";
        }
        finally { IsCreating = false; }
    }

    private bool CanCreate() => !IsCreating && !IsScanning;
}
