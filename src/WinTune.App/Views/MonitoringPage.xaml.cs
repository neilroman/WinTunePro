using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class MonitoringPage : Page
{
    public MonitoringViewModel ViewModel { get; } =
        App.Services.GetRequiredService<MonitoringViewModel>();

    public MonitoringPage() => InitializeComponent();
}
