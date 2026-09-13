using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class MonitoringViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private string _status = "Listo para escanear";
    [ObservableProperty] private IReadOnlyList<Finding> _findings = [];

    private ChangeSet? _pendingChangeSet;

    public MonitoringViewModel(ModuleOrchestrator orchestrator, BrokerClient broker)
    {
        _orchestrator = orchestrator;
        _broker = broker;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Status = "Detectando procesos con alto consumo…";
        _pendingChangeSet = null;

        try
        {
            var module = _orchestrator.Modules.FirstOrDefault(m => m.Metadata.Id == "wintune.monitoring");
            if (module is null) { Status = "Módulo no encontrado"; return; }

            Findings = await module.ScanAsync(new ScanContext(), ct);
            _pendingChangeSet = await module.PreviewAsync(Findings, ct);
            Status = Findings.Count > 0
                ? $"{Findings.Count} procesos con alto consumo detectados"
                : "Sin procesos problemáticos detectados";
        }
        catch (OperationCanceledException) { Status = "Cancelado"; }
        finally { IsScanning = false; }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        if (_pendingChangeSet is null) return;
        IsApplying = true;
        Status = "Terminando procesos seleccionados…";

        try
        {
            var module = _orchestrator.Modules.First(m => m.Metadata.Id == "wintune.monitoring");
            var result = await module.ApplyAsync(_pendingChangeSet, CancellationToken.None);
            Status = result.Success
                ? $"Completado — {result.Applied.Count} procesos terminados"
                : $"Completado con errores — {result.Failed.Count} fallidos";
            _pendingChangeSet = null;
            Findings = [];
        }
        finally { IsApplying = false; }
    }

    private bool CanApply() => _pendingChangeSet is not null && !IsApplying && !IsScanning;
}
