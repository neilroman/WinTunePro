using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class GamingPage : Page
{
    public GamingViewModel ViewModel { get; } =
        App.Services.GetRequiredService<GamingViewModel>();

    public GamingPage() => InitializeComponent();
}
