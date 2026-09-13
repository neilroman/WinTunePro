using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;

namespace WinTune.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _svc;
    private readonly BrokerClient _broker;

    private static readonly string[] Themes = ["System", "Light", "Dark"];

    [ObservableProperty] private int _selectedTheme;
    [ObservableProperty] private bool _notificationsEnabled = true;
    [ObservableProperty] private bool _autoScanEnabled;
    [ObservableProperty] private string _status = string.Empty;

    public SettingsViewModel(SettingsService svc, BrokerClient broker)
    {
        _svc = svc;
        _broker = broker;

        var settings = _svc.Load();
        var themeIndex = Array.IndexOf(Themes, settings.AppTheme);
        SelectedTheme = themeIndex >= 0 ? themeIndex : 0;
        NotificationsEnabled = settings.NotificationsEnabled;
        AutoScanEnabled = settings.AutoScanEnabled;
    }

    partial void OnSelectedThemeChanged(int value)
    {
        // theme change applied in code-behind
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        var theme = SelectedTheme >= 0 && SelectedTheme < Themes.Length
            ? Themes[SelectedTheme]
            : Themes[0];
        var settings = new AppSettings
        {
            AppTheme = theme,
            NotificationsEnabled = NotificationsEnabled,
            AutoScanEnabled = AutoScanEnabled
        };

        _svc.Save(settings);
        Status = "Configuración guardada.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        Status = "Creando punto de restauración…";
        var response = await _broker.CreateRestorePointAsync();
        Status = response.Ok
            ? "Punto de restauración creado."
            : $"Error al crear punto de restauración: {response.Error}";
    }
}
