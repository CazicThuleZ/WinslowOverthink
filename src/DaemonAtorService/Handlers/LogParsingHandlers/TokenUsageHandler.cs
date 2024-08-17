
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class TokenUsageHandler : ILogProcessor
{
    private readonly ILogger<TokenUsageHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;

    public TokenUsageHandler(ILogger<TokenUsageHandler> logger, HttpClient httpClient, IOptions<GlobalSettings> globalSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
    }
    public async Task<bool> ProcessAsync(string fileName)
    {

        _logger.LogInformation("Processing token usage log");

        try
        {
            bool success = true;

            var logTokenUsage = await DeserializeLogTokenUsageAsync(fileName);
            if (logTokenUsage == null)
                success = false;
            else
                success = await PostLogTokenUsageAsync(logTokenUsage);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing token usage log: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> PostLogTokenUsageAsync(LogTokenUsage logTokenUsage)
    {
        bool success = true;

        ActivityCountDto activityCountDto = new ActivityCountDto
        {
            Date = logTokenUsage.SnapshotDateUTC,
            Name = "TokenUsedModel" + logTokenUsage.Model,
            Count = logTokenUsage.TotalTokens
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonData = JsonSerializer.Serialize(activityCountDto, jsonOptions);

        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_dashboardUrl + @"/activity/add-or-update-activityCount", content);

        if (response.IsSuccessStatusCode)
        {
            success = true;
            _logger.LogInformation($"Successfully added ai usage token count {activityCountDto.Name}");
        }
        else
        {
            success = false;
            _logger.LogError($"Failed to add ai usage token count {activityCountDto.Name})");
        }

        return success;
    }

    private async Task<LogTokenUsage> DeserializeLogTokenUsageAsync(string fileName)
    {
        try
        {
            string jsonString = await File.ReadAllTextAsync(fileName);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LogTokenUsage logTokenUsage = JsonSerializer.Deserialize<LogTokenUsage>(jsonString, options);

            return logTokenUsage;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing token usage log: {ex.Message}");
            return null;
        }
    }
}
