using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinTune.Broker;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(opts => opts.ServiceName = "WinTuneBroker")
    .ConfigureLogging(l =>
    {
        l.AddEventLog(e => e.SourceName = "WinTune Broker");
        l.AddConsole();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<BrokerWorker>();
        services.AddSingleton<PipeServer>();
        services.AddSingleton<PrivilegedOpsHandler>();
    })
    .Build();

await host.RunAsync();
