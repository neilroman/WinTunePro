using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class PowerPage : Page
{
    public PowerViewModel ViewModel { get; } =
        App.Services.GetRequiredService<PowerViewModel>();

    public PowerPage() => InitializeComponent();
}
