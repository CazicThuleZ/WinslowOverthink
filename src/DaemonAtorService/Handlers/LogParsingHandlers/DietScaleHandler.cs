namespace DaemonAtorService;

public class DietScaleHandler : ILogProcessor
{
    private readonly ILogger<DietScaleHandler> _logger;

    public DietScaleHandler(ILogger<DietScaleHandler> logger)
    {
        _logger = logger;
    }

public async Task<bool> ProcessAsync(string fileName)
    {
        _logger.LogInformation("Processing Daily weigh in");
        await Task.CompletedTask;

        return false;
    }
}
