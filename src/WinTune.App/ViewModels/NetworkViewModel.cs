using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class NetworkViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _status = "Listo para analizar";
    [ObservableProperty] private IReadOnlyList<Finding> _findings = [];

    public NetworkViewModel(ModuleOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Status = "Analizando configuración de red…";

        try
        {
            var module = _orchestrator.Modules.FirstOrDefault(m => m.Metadata.Id == "wintune.network");
            if (module is null) { Status = "Módulo no encontrado"; return; }

            Findings = await module.ScanAsync(new ScanContext(), ct);
            Status = Findings.Count == 0
                ? "Red configurada correctamente"
                : $"{Findings.Count} recomendaciones encontradas";
        }
        catch (OperationCanceledException) { Status = "Cancelado"; }
        finally { IsScanning = false; }
    }
}
