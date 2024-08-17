using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class DietScaleHandler : ILogProcessor
{
    private readonly ILogger<DietScaleHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;

    public DietScaleHandler(ILogger<DietScaleHandler> logger, HttpClient httpClient, IOptions<GlobalSettings> globalSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
    }

    public async Task<bool> ProcessAsync(string fileName)
    {
        _logger.LogInformation("Processing diet scale summary log");

        try
        {
            bool success = true;

            var logScaleWeight = await DeserializeLogWeightAsync(fileName);
            logScaleWeight ??= await DeserializeLogWeightOLDAsync(fileName);

            if (logScaleWeight == null)
                success = false;
            else
                success = await PutWeightLogAsync(logScaleWeight);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing Crypto log: {ex.Message}");
            return false;
        }
    }

    private async Task<LogScaleWeight> DeserializeLogWeightOLDAsync(string fileName)
    {

        // TODO:  mostly sure this isn't needed any more.
        try
        {
            string fileContent = await File.ReadAllTextAsync(fileName);

            string dateParsed = string.Empty;
            string weightParsed = string.Empty;
            string weightPattern = @"Weight as of (?<date>\d{8}\d{6}) is: (?<weight>[0-9.]+) lbs";

            var weightMatch = Regex.Match(fileContent, weightPattern);
            if (weightMatch.Success)
            {

                dateParsed = weightMatch.Groups["date"].Value;
                weightParsed = weightMatch.Groups["weight"].Value;
                DateTime dateTimeNormal;
                var ableToParse = DateTime.TryParseExact(dateParsed, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeNormal);

                var weight = decimal.Parse(weightParsed);

                return new LogScaleWeight
                {
                    SnapshotDateUTC = dateTimeNormal,
                    Weight = weight
                };
            }
            else
            {
                _logger.LogError("Error parsing scale weight log: Invalid file format.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing scale weight log: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> PutWeightLogAsync(LogScaleWeight logScaleWeight)
    {
        bool success = true;
        var date = logScaleWeight.SnapshotDateUTC.Date;
        var weight = logScaleWeight.Weight;

        WeightUpdateDto weightUpdateDto = new WeightUpdateDto
        {
            Date = date.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Weight = weight
        };

        var updateWeight = JsonSerializer.Serialize(weightUpdateDto);

        var request = new HttpRequestMessage(HttpMethod.Put, _dashboardUrl + @"/diet/update-weight")
        {
            Content = new StringContent(updateWeight, Encoding.UTF8, "application/json")
        };

        using (var response = await _httpClient.SendAsync(request))
        {

            if (response.IsSuccessStatusCode)
            {
                success = true;
                _logger.LogInformation($"Successfully added daily weight {logScaleWeight.SnapshotDateUTC}");
            }
            else
            {
                success = false;
                _logger.LogError($"Failed to add daily weight for {logScaleWeight.SnapshotDateUTC})");
            }
        }

        return success;
    }

    private async Task<LogScaleWeight> DeserializeLogWeightAsync(string fileName)
    {
        try
        {
            string jsonString = await File.ReadAllTextAsync(fileName);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LogScaleWeight logScaleWeight = JsonSerializer.Deserialize<LogScaleWeight>(jsonString, options);

            return logScaleWeight;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing scale weight log: {ex.Message}");
            return null;
        }
    }
}