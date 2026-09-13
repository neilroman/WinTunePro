using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinTune.Broker.Contracts;

namespace WinTune.App.Services;

public sealed class BrokerClient(ILogger<BrokerClient> log)
{
    public async Task<RpcResponse> SendAsync(RpcRequest request, CancellationToken ct = default)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", BrokerMethods.PipeName,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(3000, ct);

            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, leaveOpen: true);

            await writer.WriteLineAsync(JsonSerializer.Serialize(request));
            var line = await reader.ReadLineAsync(ct);
            return line is null
                ? new RpcResponse(request.Id, false, null, "Empty response")
                : JsonSerializer.Deserialize<RpcResponse>(line)
                  ?? new RpcResponse(request.Id, false, null, "Parse error");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Broker call failed: {Method}", request.Method);
            return new RpcResponse(request.Id, false, null, ex.Message);
        }
    }

    public Task<RpcResponse> PingAsync(CancellationToken ct = default) =>
        SendAsync(RpcRequest.Create(BrokerMethods.Ping), ct);

    public Task<RpcResponse> CreateRestorePointAsync(CancellationToken ct = default) =>
        SendAsync(RpcRequest.Create(BrokerMethods.CreateRestorePoint), ct);

    public Task<RpcResponse> DeleteFilesAsync(IEnumerable<string> paths, CancellationToken ct = default) =>
        SendAsync(RpcRequest.Create(BrokerMethods.DeleteFiles, paths.ToArray()), ct);

    public Task<RpcResponse> FlushDnsAsync(CancellationToken ct = default) =>
        SendAsync(RpcRequest.Create(BrokerMethods.FlushDns), ct);
}
