using Quartz;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

[DisallowConcurrentExecution]
public class CryptoPriceJob : IJob
{
    private readonly ILogger<CryptoPriceJob> _logger;
    private readonly RestClient _client;
    private readonly string _filePath;
    private readonly string _held_Currencies;
    private readonly ILoggingStrategy _loggingStrategy;
    public CryptoPriceJob(ILogger<CryptoPriceJob> logger, IOptions<CryptoSettings> settings, ILoggingStrategy loggingStrategy)
    {
        _logger = logger;
        _loggingStrategy = loggingStrategy;
        var cryptoSettings = settings.Value;
        _client = new RestClient(cryptoSettings.ApiBaseUrl);
        _filePath = cryptoSettings.FilePath;
        _held_Currencies = cryptoSettings.HeldCurrencies;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("CryptoPriceJob started at {time}", DateTimeOffset.Now);

        var request = new RestRequest("coins/markets", Method.Get);
        request.AddParameter("vs_currency", "usd");
        request.AddParameter("ids", _held_Currencies);

        var response = await _client.ExecuteAsync(request);
        if (response.IsSuccessful)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringLongConverter()
                }
            };
            var cryptoStats = JsonSerializer.Deserialize<List<CryptoStat>>(response.Content, options);
            await SaveCryptoPrices(cryptoStats);
        }
        else
        {
            throw new Exception(response.ErrorMessage);
        }

        _logger.LogInformation("CryptoPriceJob completed at {time}", DateTimeOffset.Now);
    }

    private async Task SaveCryptoPrices(List<CryptoStat> cryptoStats)
    {
        var today = DateTime.UtcNow.Date;

        // Load the existing history
        List<CryptoHistory> history = LoadCryptoHistory();

        // Iterate over the new stats and update the history
        foreach (var stat in cryptoStats)
        {
            // Find or create the history for the current crypto
            var cryptoHistory = history.FirstOrDefault(h => h.Id == stat.Id);
            if (cryptoHistory == null)
            {
                cryptoHistory = new CryptoHistory { Id = stat.Id, Symbol = stat.Symbol };
                history.Add(cryptoHistory);
            }

            // Find or create the daily stat for today
            var dailyStat = cryptoHistory.DailyStats.FirstOrDefault(ds => ds.Date == today);
            if (dailyStat == null)
            {
                dailyStat = new DailyCryptoStat { Date = today, HighestPrice = stat.Current_Price };
                cryptoHistory.DailyStats.Add(dailyStat);
            }
            else
            {
                // Update the highest price if the new price is higher
                if (stat.Current_Price > dailyStat.HighestPrice)
                {
                    dailyStat.HighestPrice = stat.Current_Price;
                }
            }
            // // Remove records older than 14 days
            // cryptoHistory.DailyStats = cryptoHistory.DailyStats
            //     .Where(ds => ds.Date >= today.AddDays(-14))
            //     .ToList();
        }

        // Save peak values for dates not equal to today and remove them from the history
        foreach (var cryptoHistory in history)
        {
            var pastDailyStats = cryptoHistory.DailyStats
                .Where(ds => ds.Date != today)
                .ToList();

            foreach (var pastDailyStat in pastDailyStats)
            {

                LogCrypto logCrypto = new()
                {
                    Id = cryptoHistory.Id,
                    Symbol = cryptoHistory.Symbol,
                    PriceDateUTC = pastDailyStat.Date,
                    HighestPrice = pastDailyStat.HighestPrice
                };

                _loggingStrategy.Log(logCrypto);
                cryptoHistory.DailyStats.Remove(pastDailyStat);
                await Task.Delay(2000);  // just to ensure unique filenames.
            }
        }

        // Save the cleaned-up history back to the file
        SaveCryptoHistory(history);
    }

    private List<CryptoHistory> LoadCryptoHistory()
    {
        if (!File.Exists(_filePath))
        {
            return new List<CryptoHistory>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<CryptoHistory>>(json);
    }

    private void SaveCryptoHistory(List<CryptoHistory> history)
    {
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
