using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using WinTune.App.Services;
using WinTune.App.ViewModels;
using WinTune.Core.Orchestrator;
using WinTune.Core.PluginHost;
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
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    private static ServiceProvider BuildServices()
    {
        var svc = new ServiceCollection();

        // Plugin host — loads first-party plugins
        var pluginHost = new AssemblyPluginHost(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AssemblyPluginHost>.Instance);
        var modules = pluginHost.LoadFromDirectory(AppContext.BaseDirectory);

        svc.AddSingleton(pluginHost);
        svc.AddSingleton<IEnumerable<IOptimizerModule>>(modules);
        svc.AddSingleton<ModuleOrchestrator>();

        // App services
        svc.AddSingleton<BrokerClient>();
        svc.AddSingleton<NavigationService>();

        // ViewModels
        svc.AddTransient<DashboardViewModel>();
        svc.AddTransient<CleanupViewModel>();
        svc.AddTransient<PerformanceViewModel>();
        svc.AddTransient<PrivacyViewModel>();

        svc.AddLogging();
        return svc.BuildServiceProvider();
    }
}
