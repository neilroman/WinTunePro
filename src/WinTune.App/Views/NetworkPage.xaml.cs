using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; } =
        App.Services.GetRequiredService<NetworkViewModel>();

    public NetworkPage() => InitializeComponent();
}
