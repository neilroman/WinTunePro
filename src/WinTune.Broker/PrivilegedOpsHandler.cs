using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinTune.Broker.Contracts;

namespace WinTune.Broker;

public sealed class PrivilegedOpsHandler(ILogger<PrivilegedOpsHandler> log)
{
    public async Task<RpcResponse> HandleAsync(RpcRequest req, CancellationToken ct)
    {
        try
        {
            var result = req.Method switch
            {
                BrokerMethods.Ping => "pong",
                BrokerMethods.CreateRestorePoint => await CreateRestorePointAsync(req, ct),
                BrokerMethods.DeleteFiles => await DeleteFilesAsync(req, ct),
                BrokerMethods.FlushDns => await FlushDnsAsync(ct),
                BrokerMethods.RunDism => await RunDismCleanupAsync(ct),
                BrokerMethods.GetSystemInfo => GetSystemInfo(),
                _ => throw new NotSupportedException($"Unknown method: {req.Method}")
            };

            return new RpcResponse(req.Id, true, JsonSerializer.Serialize(result), null);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Handler error for {Method}", req.Method);
            return new RpcResponse(req.Id, false, null, ex.Message);
        }
    }

    private static async Task<string> CreateRestorePointAsync(RpcRequest req, CancellationToken ct)
    {
        var ps = $"Checkpoint-Computer -Description 'WinTune Pro' -RestorePointType 'MODIFY_SETTINGS'";
        await RunPowerShellAsync(ps, ct);
        return "restore_point_created";
    }

    private static async Task<string> DeleteFilesAsync(RpcRequest req, CancellationToken ct)
    {
        if (req.PayloadJson is null) return "no_paths";
        var paths = JsonSerializer.Deserialize<string[]>(req.PayloadJson) ?? [];
        int deleted = 0;
        foreach (var p in paths)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (File.Exists(p)) { File.Delete(p); deleted++; }
                else if (Directory.Exists(p)) { Directory.Delete(p, true); deleted++; }
            }
            catch { /* best-effort */ }
        }
        return $"{deleted}_deleted";
    }

    private static async Task<string> FlushDnsAsync(CancellationToken ct)
    {
        await RunCommandAsync("ipconfig", "/flushdns", ct);
        return "dns_flushed";
    }

    private static async Task<string> RunDismCleanupAsync(CancellationToken ct)
    {
        await RunCommandAsync("dism", "/Online /Cleanup-Image /StartComponentCleanup", ct);
        return "dism_done";
    }

    private static string GetSystemInfo()
    {
        return JsonSerializer.Serialize(new
        {
            os = Environment.OSVersion.ToString(),
            processors = Environment.ProcessorCount,
            memory_mb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024),
            machine = Environment.MachineName,
        });
    }

    private static async Task RunPowerShellAsync(string script, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync(ct);
    }

    private static async Task RunCommandAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync(ct);
    }
}
