namespace DaemonAtorService;

public class LogHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LogHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILogProcessor GetFileProcessor(LogFileFormat format)
    {
        return format switch
        {
            LogFileFormat.LoseItDailySummary => _serviceProvider.GetRequiredService<LoseItDailySummaryHandler>(),
            LogFileFormat.BalanceAlerts => _serviceProvider.GetRequiredService<BankAccountBalanceHandler>(),
            LogFileFormat.CryptoPrice => _serviceProvider.GetRequiredService<CryptoPriceHandler>(),
            LogFileFormat.DietScale => _serviceProvider.GetRequiredService<DietScaleHandler>(),
            LogFileFormat.ActivityDuration => _serviceProvider.GetRequiredService<ActivityDurationHandler>(),
            LogFileFormat.ActivityCounter => _serviceProvider.GetRequiredService<ActivityCountHandler>(),
            LogFileFormat.TokenUsage => _serviceProvider.GetRequiredService<TokenUsageHandler>(),
            LogFileFormat.Other => _serviceProvider.GetRequiredService<OtherLogHandler>(),
            _ => throw new NotImplementedException($"No processor available for format: {format}")
        };
    }
}
