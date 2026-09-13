using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class PowerViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private string _status = "Listo para escanear";
    [ObservableProperty] private IReadOnlyList<Finding> _findings = [];

    private ChangeSet? _pendingChangeSet;

    public PowerViewModel(ModuleOrchestrator orchestrator, BrokerClient broker)
    {
        _orchestrator = orchestrator;
        _broker = broker;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Status = "Analizando plan de energía…";
        _pendingChangeSet = null;

        try
        {
            var module = _orchestrator.Modules.FirstOrDefault(m => m.Metadata.Id == "wintune.power");
            if (module is null) { Status = "Módulo no encontrado"; return; }

            Findings = await module.ScanAsync(new ScanContext(), ct);
            _pendingChangeSet = await module.PreviewAsync(Findings, ct);
            Status = Findings.Count > 0
                ? $"{Findings.Count} recomendaciones de energía encontradas"
                : "Plan de energía óptimo";
        }
        catch (OperationCanceledException) { Status = "Cancelado"; }
        finally { IsScanning = false; }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        if (_pendingChangeSet is null) return;
        IsApplying = true;
        Status = "Aplicando configuración de energía…";

        try
        {
            var module = _orchestrator.Modules.First(m => m.Metadata.Id == "wintune.power");
            var result = await module.ApplyAsync(_pendingChangeSet, CancellationToken.None);
            Status = result.Success
                ? "Plan de energía actualizado"
                : $"Error al aplicar: {result.Failed.Count} cambios fallidos";
            _pendingChangeSet = null;
            Findings = [];
        }
        finally { IsApplying = false; }
    }

    private bool CanApply() => _pendingChangeSet is not null && !IsApplying && !IsScanning;
}
