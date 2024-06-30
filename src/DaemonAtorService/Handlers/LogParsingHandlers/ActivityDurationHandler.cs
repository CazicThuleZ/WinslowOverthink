namespace DaemonAtorService;

public class ActivityDurationHandler : ILogProcessor
{
    private readonly ILogger<ActivityDurationHandler> _logger;

    public ActivityDurationHandler(ILogger<ActivityDurationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(string fileName)
    {
        _logger.LogInformation("Processing activity duration");
        await Task.CompletedTask;

        return false;
    }

}
