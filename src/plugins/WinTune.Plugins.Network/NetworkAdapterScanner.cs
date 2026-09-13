using System.Net.NetworkInformation;
using WinTune.Sdk;

namespace WinTune.Plugins.Network;

internal sealed class NetworkAdapterScanner : IScanTask
{
    public string Name => "Adaptadores de red";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var ghostAdapters = interfaces
                .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback
                         && i.OperationalStatus == OperationalStatus.Down
                         && i.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            if (ghostAdapters.Count > 0)
            {
                findings.Add(Finding.Create(
                    moduleId: "wintune.network",
                    title: $"{ghostAdapters.Count} adaptador(es) desconectado(s)",
                    description: $"Adaptadores inactivos: {string.Join(", ", ghostAdapters.Select(a => a.Name))}. Deshabilitarlos puede reducir el tiempo de inicio de red.",
                    severity: FindingSeverity.Info,
                    metadata: new Dictionary<string, object>
                    {
                        ["adapters"] = ghostAdapters.Select(a => a.Name).ToList(),
                    }));
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
