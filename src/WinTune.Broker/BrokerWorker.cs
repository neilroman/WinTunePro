using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WinTune.Broker;

public sealed class BrokerWorker(PipeServer pipe, ILogger<BrokerWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        log.LogInformation("WinTune Broker started.");
        await pipe.ListenAsync(ct);
        log.LogInformation("WinTune Broker stopped.");
    }
}
