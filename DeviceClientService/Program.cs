using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddEventLog(eventLogSettings =>
        {
            eventLogSettings.SourceName = "DeviceClientService";
        });
    })
    .Build();

await host.RunAsync();