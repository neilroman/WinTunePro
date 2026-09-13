using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.Ai;
using WinTune.App.Services;
using WinTune.Core.Orchestrator;
using WinTune.Monitor;
using WinTune.Sdk;
using AppNotificationService = WinTune.App.Services.NotificationService;

namespace WinTune.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ModuleOrchestrator _orchestrator;
    private readonly BrokerClient _broker;
    private readonly UsagePatternAnalyzer _analyzer;
    private readonly MetricRingBuffer _metrics;
    private readonly AppNotificationService _notifications;
    private readonly HtmlReportService _htmlReport;
    private OrchestratorScanResult? _lastResult;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _healthScore = 100;
    [ObservableProperty] private string _statusMessage = "Listo";
    [ObservableProperty] private long _totalBytesSaved;
    [ObservableProperty] private int _totalFindings;
    [ObservableProperty] private IReadOnlyList<ScanRecommendation> _recommendations = [];
    [ObservableProperty] private float _cpuPercent;
    [ObservableProperty] private float _ramPercent;

    public DashboardViewModel(ModuleOrchestrator orchestrator, BrokerClient broker,
                              UsagePatternAnalyzer analyzer, MetricRingBuffer metrics,
                              AppNotificationService notifications, HtmlReportService htmlReport)
    {
        _orchestrator = orchestrator;
        _broker       = broker;
        _analyzer     = analyzer;
        _metrics      = metrics;
        _notifications= notifications;
        _htmlReport   = htmlReport;
        RefreshRecommendations();
        StartMetricPolling();
    }

    private bool CanExport => _lastResult != null && !IsScanning;

    partial void OnIsScanningChanged(bool _) => ExportReportCommand.NotifyCanExecuteChanged();

    private void StartMetricPolling()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                var s = _metrics.Latest;
                float cpu = s.CpuPercent;
                float ram = s.RamTotalMb > 0 ? s.RamUsedMb / s.RamTotalMb * 100f : 0f;
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CpuPercent = cpu;
                    RamPercent = ram;
                });
            }
        });
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

            var allFindings = result.Results.SelectMany(r => r.Findings).ToList();
            int critical = allFindings.Count(f => f.Severity == FindingSeverity.Critical);
            int high     = allFindings.Count(f => f.Severity == FindingSeverity.High);
            _notifications.NotifyFindings(critical, high, TotalFindings);

            _lastResult = result;
            ExportReportCommand.NotifyCanExecuteChanged();
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

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportReportAsync()
    {
        if (_lastResult is null) return;
        var rows = _lastResult.Results
            .SelectMany(r => r.Findings.Select(f => new FindingRow(
                r.ModuleId,
                f.Severity.ToString(),
                f.Title,
                f.Description ?? string.Empty)))
            .ToList();

        var html = _htmlReport.BuildReport(
            DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            rows,
            HealthScore);

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"WinTunePro-{DateTime.Now:yyyyMMdd-HHmmss}.html");

        await File.WriteAllTextAsync(path, html);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        StatusMessage = $"Reporte exportado → {Path.GetFileName(path)}";
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
