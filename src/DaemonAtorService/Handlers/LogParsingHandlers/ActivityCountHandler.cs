namespace DaemonAtorService;

public class ActivityCountHandler : ILogProcessor
{
    private readonly ILogger<ActivityCountHandler> _logger;
    public ActivityCountHandler(ILogger<ActivityCountHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(string fileName)
    {
        _logger.LogInformation("Processing activity count");
        await Task.CompletedTask;

        return false;
    }

}
