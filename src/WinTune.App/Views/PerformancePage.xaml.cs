using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class PerformancePage : Page
{
    public PerformanceViewModel ViewModel { get; } =
        App.Services.GetRequiredService<PerformanceViewModel>();

    public PerformancePage() => InitializeComponent();
}
