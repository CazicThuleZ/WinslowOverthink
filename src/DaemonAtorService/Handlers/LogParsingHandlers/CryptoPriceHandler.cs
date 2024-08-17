
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class CryptoPriceHandler : ILogProcessor
{
    private readonly ILogger<CryptoPriceHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;
    public CryptoPriceHandler(ILogger<CryptoPriceHandler> logger, HttpClient httpClient, IOptions<GlobalSettings> globalSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
    }
    public async Task<bool> ProcessAsync(string fileName)
    {
        _logger.LogInformation("Processing crypto currency summary log");

        try
        {
            bool success = true;

            var logCrypto = await DeserializeLogCryptoAsync(fileName);
            if (logCrypto == null)
                success = false;
            else
                success = await PostCryptoLogAsync(logCrypto);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing Crypto log: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> PostCryptoLogAsync(LogCrypto logCrypto)
    {
        bool success = true;

        CryptoPriceDto cryptoPriceDto = new CryptoPriceDto
        {
            SnapshotDateUTC = logCrypto.PriceDateUTC,
            CryptoId = logCrypto.Symbol,
            Price = logCrypto.HighestPrice
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonData = JsonSerializer.Serialize(cryptoPriceDto, jsonOptions);

        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_dashboardUrl + @"/crypto/create-crypto-price-snapshot", content);

        if (response.IsSuccessStatusCode)
        {
            success = true;
            _logger.LogInformation($"Successfully added crypto price {logCrypto.Symbol} at {logCrypto.PriceDateUTC}");
        }
        else
        {
            success = false;
            _logger.LogError($"Failed to add food price for {logCrypto.Symbol})");
        }

        return success;

    }
    private async Task<LogCrypto> DeserializeLogCryptoAsync(string fileName)
    {
        try
        {
            string jsonString = await File.ReadAllTextAsync(fileName);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LogCrypto logCrypto = JsonSerializer.Deserialize<LogCrypto>(jsonString, options);

            return logCrypto;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing crypto log: {ex.Message}");
            return null;
        }
    }
}
