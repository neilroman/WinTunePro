using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinTune.App.Views;

namespace WinTune.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var tag = (e.SelectedItemContainer as NavigationViewItem)?.Tag?.ToString();
        Type? page = tag switch
        {
            "dashboard"   => typeof(DashboardPage),
            "cleanup"     => typeof(CleanupPage),
            "performance" => typeof(PerformancePage),
            "privacy"     => typeof(PrivacyPage),
            _             => typeof(DashboardPage),
        };
        ContentFrame.Navigate(page);
    }
}
