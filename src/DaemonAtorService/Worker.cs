using Quartz;
using Quartz.Spi;

namespace DaemonAtorService;

public class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
