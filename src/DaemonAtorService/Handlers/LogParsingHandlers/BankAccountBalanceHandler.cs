namespace DaemonAtorService;

public class BankAccountBalanceHandler : ILogProcessor
{
    private readonly ILogger<BankAccountBalanceHandler> _logger;

    public BankAccountBalanceHandler(ILogger<BankAccountBalanceHandler> logger)
    {
        _logger = logger;
    }
public async Task<bool> ProcessAsync(string fileName)
    {
        _logger.LogInformation("Processing account balance notice");
        await Task.CompletedTask;

        return false;
    }
}
