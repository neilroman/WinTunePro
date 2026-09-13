using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinTune.Broker.Contracts;

namespace WinTune.Broker;

public sealed class PipeServer(PrivilegedOpsHandler ops, ILogger<PipeServer> log)
{
    public const string PipeName = "WinTuneBroker";

    public async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough);

                await pipe.WaitForConnectionAsync(ct);
                _ = HandleClientAsync(pipe, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { log.LogError(ex, "Pipe listen error"); }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using (pipe)
        {
            try
            {
                using var reader = new StreamReader(pipe, leaveOpen: true);
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

                var line = await reader.ReadLineAsync(ct);
                if (line is null) return;

                var req = JsonSerializer.Deserialize<RpcRequest>(line);
                if (req is null) return;

                var resp = await ops.HandleAsync(req, ct);
                await writer.WriteLineAsync(JsonSerializer.Serialize(resp));
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Client handler error");
            }
        }
    }
}
