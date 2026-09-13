using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace WinTune.App.Services;

public sealed class NotificationService : IDisposable
{
    private bool _registered;

    public void Register()
    {
        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch
        {
            // Unpackaged app without Start Menu shortcut — notifications unavailable
        }
    }

    public void NotifyFindings(int critical, int high, int total)
    {
        if (!_registered) return;
        if (critical == 0 && high == 0) return;

        var title = critical > 0
            ? $"{critical} problema(s) crítico(s) detectado(s)"
            : $"{high} problema(s) importantes detectados";

        var body = $"WinTune Pro encontró {total} problemas en total. Abre la app para aplicar las correcciones.";

        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .AddArgument("action", "open")
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch { /* non-critical */ }
    }

    private static void OnNotificationInvoked(AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            App.MainWindow.Activate());
    }

    public void Dispose()
    {
        if (_registered)
            AppNotificationManager.Default.Unregister();
    }
}
