using System.IO.Compression;
using System.Text.Json;

namespace WinTune.App.Services;

public record PluginCatalogEntry(
    string Name, string Id, string Description,
    string Version, string DownloadUrl, string Author);

public sealed class MarketplaceService(IHttpClientFactory factory)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<PluginCatalogEntry>> FetchCatalogAsync(
        string catalogUrl, CancellationToken ct)
    {
        try
        {
            var client = factory.CreateClient();
            await using var stream = await client.GetStreamAsync(catalogUrl, ct).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<List<PluginCatalogEntry>>(
                       stream, JsonOpts, ct).ConfigureAwait(false) ?? [];
        }
        catch { return []; }
    }

    public async Task<bool> InstallAsync(
        PluginCatalogEntry entry, string pluginsDir, CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"{entry.Id}_{Guid.NewGuid():N}.nupkg");
        try
        {
            var client = factory.CreateClient();
            await using var src = await client.GetStreamAsync(entry.DownloadUrl, ct).ConfigureAwait(false);
            await using (var dst = File.Create(tmp))
                await src.CopyToAsync(dst, ct).ConfigureAwait(false);

            Directory.CreateDirectory(pluginsDir);
            using var zip = ZipFile.OpenRead(tmp);
            var dll = FindDll(zip);
            if (dll is null) return false;

            dll.ExtractToFile(Path.Combine(pluginsDir, $"{entry.Id}.dll"), overwrite: true);
            return true;
        }
        catch { return false; }
        finally { try { File.Delete(tmp); } catch { } }
    }

    private static ZipArchiveEntry? FindDll(ZipArchive z)
    {
        ZipArchiveEntry? Prefix(string p) => z.Entries.FirstOrDefault(e =>
            e.FullName.StartsWith(p, StringComparison.OrdinalIgnoreCase) &&
            e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

        return Prefix("lib/net9.0/")
            ?? Prefix("lib/net8.0/")
            ?? z.Entries.FirstOrDefault(e =>
                !e.FullName.Contains('/') && !e.FullName.Contains('\\') &&
                e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
    }
}
