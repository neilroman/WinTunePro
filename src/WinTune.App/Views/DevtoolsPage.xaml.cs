using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class DevtoolsPage : Page
{
    public DevtoolsViewModel ViewModel { get; } =
        App.Services.GetRequiredService<DevtoolsViewModel>();

    public DevtoolsPage() => InitializeComponent();
}
