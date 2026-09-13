using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class BackupPage : Page
{
    public BackupViewModel ViewModel { get; } =
        App.Services.GetRequiredService<BackupViewModel>();

    public BackupPage() => InitializeComponent();
}
