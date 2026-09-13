using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class MarketplacePage : Page
{
    public MarketplaceViewModel ViewModel { get; } =
        App.Services.GetRequiredService<MarketplaceViewModel>();

    public MarketplacePage() => InitializeComponent();

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Plugins.Count == 0)
            ViewModel.LoadCommand.Execute(null);
    }
}
