using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.Ai;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;

namespace WinTune.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;
    private readonly UsagePatternAnalyzer _analyzer;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _healthScore = 100;
    [ObservableProperty] private string _statusMessage = "Listo";
    [ObservableProperty] private long _totalBytesSaved;
    [ObservableProperty] private int _totalFindings;
    [ObservableProperty] private IReadOnlyList<ScanRecommendation> _recommendations = [];

    public DashboardViewModel(ModuleOrchestrator orchestrator, BrokerClient broker, UsagePatternAnalyzer analyzer)
    {
        _orchestrator = orchestrator;
        _broker = broker;
        _analyzer = analyzer;
        RefreshRecommendations();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAllAsync(CancellationToken ct)
    {
        IsScanning = true;
        StatusMessage = "Escaneando…";
        TotalFindings = 0;
        TotalBytesSaved = 0;

        try
        {
            var progress = new Progress<ModuleScanProgress>(p =>
                StatusMessage = $"[{p.CompletedModules}/{p.TotalModules}] {p.CurrentModule}");

            var result = await _orchestrator.ScanAllAsync(new ScanContext { Progress = null }, progress, ct);
            TotalFindings = result.TotalFindings;
            TotalBytesSaved = result.TotalBytesSaved;
            HealthScore = ComputeHealthScore(result);
            StatusMessage = $"Análisis completo — {TotalFindings} problemas encontrados";
            RefreshRecommendations();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Análisis cancelado";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void RefreshRecommendations()
    {
        var modules = _orchestrator.Modules;
        var moduleIds = modules.Select(m => m.Metadata.Id).ToList();
        var displayNames = modules.ToDictionary(m => m.Metadata.Id, m => m.Metadata.DisplayName);

        var raw = _analyzer.GetRecommendations(moduleIds);
        Recommendations = raw
            .Select(r => r with { DisplayName = displayNames.GetValueOrDefault(r.ModuleId, r.ModuleId) })
            .ToList();
    }

    private static int ComputeHealthScore(OrchestratorScanResult result)
    {
        int penalty = result.Results
            .SelectMany(r => r.Findings)
            .Sum(f => f.Severity switch
            {
                FindingSeverity.Critical => 20,
                FindingSeverity.High => 10,
                FindingSeverity.Medium => 5,
                FindingSeverity.Low => 2,
                _ => 0
            });
        return Math.Max(0, 100 - penalty);
    }
}
