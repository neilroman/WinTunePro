using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class PrivacyViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private string _status = "Listo para analizar";
    [ObservableProperty] private IReadOnlyList<Finding> _findings = [];

    private ChangeSet? _pendingChangeSet;

    public PrivacyViewModel(ModuleOrchestrator orchestrator, BrokerClient broker)
    {
        _orchestrator = orchestrator;
        _broker = broker;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Status = "Analizando configuración de privacidad…";
        _pendingChangeSet = null;

        try
        {
            var module = _orchestrator.Modules.FirstOrDefault(m => m.Metadata.Id == "wintune.privacy");
            if (module is null) { Status = "Módulo no encontrado"; return; }

            Findings = await module.ScanAsync(new ScanContext(), ct);
            _pendingChangeSet = await module.PreviewAsync(Findings, ct);
            Status = $"{Findings.Count} problema(s) de privacidad encontrado(s)";
        }
        catch (OperationCanceledException) { Status = "Cancelado"; }
        finally { IsScanning = false; }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        if (_pendingChangeSet is null) return;
        IsApplying = true;
        Status = "Aplicando cambios de privacidad… (requiere privilegios)";

        try
        {
            await _broker.CreateRestorePointAsync();
            var module = _orchestrator.Modules.First(m => m.Metadata.Id == "wintune.privacy");
            var result = await module.ApplyAsync(_pendingChangeSet, CancellationToken.None);
            Status = $"Completado — {result.Applied.Count} cambios, {result.Failed.Count} errores";
            _pendingChangeSet = null;
            Findings = [];
        }
        finally { IsApplying = false; }
    }

    private bool CanApply() => _pendingChangeSet is not null && !IsApplying && !IsScanning;
}
