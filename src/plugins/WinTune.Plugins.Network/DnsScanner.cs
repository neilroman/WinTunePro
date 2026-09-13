using Microsoft.Win32;
using WinTune.Sdk;

namespace WinTune.Plugins.Network;

internal sealed class DnsScanner : IScanTask
{
    private static readonly string[] FastPublicDns = ["8.8.8.8", "1.1.1.1", "9.9.9.9", "8.8.4.4", "1.0.0.1"];

    public string Name => "Configuración DNS";

    public Task<IReadOnlyList<Finding>> ScanAsync(CancellationToken ct)
    {
        var findings = new List<Finding>();

        try
        {
            var adaptersKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
            using var root = Registry.LocalMachine.OpenSubKey(adaptersKey);
            if (root is null) return Task.FromResult<IReadOnlyList<Finding>>(findings);

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                ct.ThrowIfCancellationRequested();
                using var sub = root.OpenSubKey(subKeyName);
                if (sub is null) continue;

                var dns = sub.GetValue("NameServer")?.ToString()
                       ?? sub.GetValue("DhcpNameServer")?.ToString();

                if (dns is null || dns.Length == 0) continue;

                var primary = dns.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (primary is not null && !FastPublicDns.Contains(primary.Trim()))
                {
                    findings.Add(Finding.Create(
                        moduleId: "wintune.network",
                        title: "DNS lento o predeterminado del ISP",
                        description: $"Adaptador usa DNS {primary}. Considera Cloudflare (1.1.1.1) o Google (8.8.8.8) para mayor velocidad.",
                        severity: FindingSeverity.Info,
                        metadata: new Dictionary<string, object>
                        {
                            ["adapterId"] = subKeyName,
                            ["currentDns"] = dns,
                        }));
                    break;
                }
            }
        }
        catch { }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
