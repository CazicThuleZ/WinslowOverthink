namespace DaemonAtorService;

public class OtherLogHandler : ILogProcessor
{
    private readonly ILogger<OtherLogHandler> _logger;

    public OtherLogHandler(ILogger<OtherLogHandler> logger)
    {
        _logger = logger;
    }

public async Task<bool> ProcessAsync(string fileName)
    {
        // The dream is for this, with the help of AI, to be able to process any log format and 
        // all other log handlers to be retired.

        _logger.LogInformation("Unknown log format detected. Skipping processing.");
        await Task.CompletedTask;

        return false;
    }
}
