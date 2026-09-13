using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class StoragePage : Page
{
    public StorageViewModel ViewModel { get; } =
        App.Services.GetRequiredService<StorageViewModel>();

    public StoragePage() => InitializeComponent();
}
