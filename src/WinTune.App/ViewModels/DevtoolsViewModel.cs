using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class DevtoolsViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private string _status = "Listo para escanear";
    [ObservableProperty] private long _estimatedBytesSaved;
    [ObservableProperty] private IReadOnlyList<Finding> _findings = [];

    private ChangeSet? _pendingChangeSet;

    public DevtoolsViewModel(ModuleOrchestrator orchestrator, BrokerClient broker)
    {
        _orchestrator = orchestrator;
        _broker = broker;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Status = "Analizando caches de desarrollo...";
        _pendingChangeSet = null;

        try
        {
            var module = _orchestrator.Modules.FirstOrDefault(m => m.Metadata.Id == "wintune.devtools");
            if (module is null) { Status = "Modulo no encontrado"; return; }

            Findings = await module.ScanAsync(new ScanContext(), ct);
            EstimatedBytesSaved = Findings.Sum(f => f.EstimatedBytesSaved);
            _pendingChangeSet = await module.PreviewAsync(Findings, ct);
            Status = $"{Findings.Count} caches detectados - {FormatBytes(EstimatedBytesSaved)} a liberar";
        }
        catch (OperationCanceledException) { Status = "Cancelado"; }
        finally { IsScanning = false; }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        if (_pendingChangeSet is null) return;
        IsApplying = true;
        Status = "Creando punto de restauracion...";

        try
        {
            await _broker.CreateRestorePointAsync();
            Status = "Limpiando caches...";

            var module = _orchestrator.Modules.First(m => m.Metadata.Id == "wintune.devtools");
            var result = await module.ApplyAsync(_pendingChangeSet, CancellationToken.None);

            long freed = result.Applied.Sum(a => a.BytesFreed);
            Status = $"Completado - {FormatBytes(freed)} liberados, {result.Failed.Count} errores";
            _pendingChangeSet = null;
            Findings = [];
        }
        finally { IsApplying = false; }
    }

    private bool CanApply() => _pendingChangeSet is not null && !IsApplying && !IsScanning;

    private static string FormatBytes(long b) => b switch
    {
        < 1024 * 1024 => $"{b / 1024.0:F0} KB",
        < 1024L * 1024 * 1024 => $"{b / (1024.0 * 1024):F1} MB",
        _ => $"{b / (1024.0 * 1024 * 1024):F2} GB"
    };
}
