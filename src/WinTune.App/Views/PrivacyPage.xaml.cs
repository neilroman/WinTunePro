using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class PrivacyPage : Page
{
    public PrivacyViewModel ViewModel { get; } =
        App.Services.GetRequiredService<PrivacyViewModel>();

    public PrivacyPage() => InitializeComponent();
}
