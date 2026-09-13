using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class CleanupPage : Page
{
    public CleanupViewModel ViewModel { get; } =
        App.Services.GetRequiredService<CleanupViewModel>();

    public CleanupPage() => InitializeComponent();
}
