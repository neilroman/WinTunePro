using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.App.Services;

namespace WinTune.App.ViewModels;

public sealed partial class MarketplacePluginItem : ObservableObject
{
    public PluginCatalogEntry Entry { get; }
    public IRelayCommand InstallCommand { get; }
    [ObservableProperty] private string _installState = "Disponible";

    public MarketplacePluginItem(PluginCatalogEntry entry, IRelayCommand installCommand)
    {
        Entry = entry;
        InstallCommand = installCommand;
    }
}

public sealed partial class MarketplaceViewModel : ObservableObject
{
    private const string CatalogUrl =
        "https://raw.githubusercontent.com/neilroman/WinTunePro/master/marketplace/catalog.json";

    private readonly MarketplaceService _svc;
    private readonly string _pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");

    [ObservableProperty] private ObservableCollection<MarketplacePluginItem> _plugins = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public MarketplaceViewModel(MarketplaceService svc) => _svc = svc;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        StatusMessage = "Cargando catálogo…";
        Plugins.Clear();
        try
        {
            var entries = await _svc.FetchCatalogAsync(CatalogUrl, ct);
            foreach (var e in entries)
            {
                if (ct.IsCancellationRequested) break;
                MarketplacePluginItem? item = null;
                var cmd = new AsyncRelayCommand(async () =>
                {
                    if (item is null) return;
                    item.InstallState = "Descargando…";
                    var ok = await _svc.InstallAsync(item.Entry, _pluginsDir, CancellationToken.None);
                    item.InstallState = ok ? "Instalado — reinicia para activar" : "Error al instalar";
                });
                item = new MarketplacePluginItem(e, cmd);
                Plugins.Add(item);
            }
            StatusMessage = Plugins.Count == 0
                ? "No hay plugins disponibles en el catálogo."
                : $"{Plugins.Count} plugin(s) disponibles.";
        }
        catch (OperationCanceledException) { StatusMessage = "Carga cancelada."; }
        catch (Exception ex)              { StatusMessage = $"Error al cargar: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
