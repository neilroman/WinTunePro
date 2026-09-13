using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; } =
        App.Services.GetRequiredService<DashboardViewModel>();

    public DashboardPage() => InitializeComponent();
}
