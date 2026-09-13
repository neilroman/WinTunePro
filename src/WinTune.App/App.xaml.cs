using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using WinTune.Ai;
using WinTune.App.Services;
using WinTune.App.ViewModels;
using WinTune.Core.Orchestrator;
using WinTune.Core.PluginHost;
using WinTune.Data;
using WinTune.Monitor;
using WinTune.Sdk;

namespace WinTune.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static MainWindow MainWindow { get; private set; } = null!;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = BuildServices();

        // Start background monitor worker (no IHost — manual start)
        _ = Services.GetRequiredService<EtwMonitorWorker>()
                    .StartAsync(CancellationToken.None);

        // Launch broker process with elevation (non-fatal if UAC is cancelled)
        try { Services.GetRequiredService<BrokerLauncher>().EnsureRunning(); }
        catch { /* UAC denied — app continues without broker */ }

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    private static ServiceProvider BuildServices()
    {
        var svc = new ServiceCollection();
        svc.AddLogging();

        // First-party plugins — registered directly so DI injects ILogger<T>
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Cleanup.CleanupModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Performance.PerformanceModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Privacy.PrivacyModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Gaming.GamingModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Storage.StorageModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Network.NetworkModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Devtools.DevtoolsModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Power.PowerModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Monitoring.MonitoringModule>();
        svc.AddSingleton<IOptimizerModule, WinTune.Plugins.Backup.BackupModule>();

        // Third-party plugins loaded from a dedicated subdirectory (empty by default)
        var externalPluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");
        var pluginHost = new AssemblyPluginHost(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AssemblyPluginHost>.Instance);
        foreach (var m in pluginHost.LoadFromDirectory(externalPluginDir))
            svc.AddSingleton(m);
        svc.AddSingleton(pluginHost);

        svc.AddSingleton<ModuleOrchestrator>();

        // Data + AI
        var journalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinTune Pro", "journal.db");
        Directory.CreateDirectory(Path.GetDirectoryName(journalPath)!);
        svc.AddSingleton(new ChangeJournal(journalPath));
        svc.AddSingleton<UsagePatternAnalyzer>();

        // Monitor
        svc.AddSingleton<MetricRingBuffer>();
        svc.AddSingleton<EtwMonitorWorker>();

        // App services
        svc.AddSingleton<BrokerClient>();
        svc.AddSingleton<BrokerLauncher>();
        svc.AddSingleton<NavigationService>();

        // ViewModels
        svc.AddTransient<DashboardViewModel>();
        svc.AddTransient<CleanupViewModel>();
        svc.AddTransient<PerformanceViewModel>();
        svc.AddTransient<PrivacyViewModel>();
        svc.AddTransient<GamingViewModel>();
        svc.AddTransient<StorageViewModel>();
        svc.AddTransient<NetworkViewModel>();
        svc.AddTransient<DevtoolsViewModel>();
        svc.AddTransient<PowerViewModel>();
        svc.AddTransient<MonitoringViewModel>();
        svc.AddTransient<BackupViewModel>();

        return svc.BuildServiceProvider();
    }
}
