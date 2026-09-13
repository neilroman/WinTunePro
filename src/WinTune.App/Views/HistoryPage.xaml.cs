using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; } =
        App.Services.GetRequiredService<HistoryViewModel>();

    public HistoryPage() => InitializeComponent();
}
