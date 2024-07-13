using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class OtherLogHandler : ILogProcessor
{
    private readonly ILogger<OtherLogHandler> _logger;
    private readonly PokeTheOracle _pokeTheOracle;
    private readonly string _dashboardUrl;
    private readonly HttpClient _httpClient;

    public OtherLogHandler(ILogger<OtherLogHandler> logger, IOptions<GlobalSettings> globalSettings, PokeTheOracle pokeTheOracle, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _pokeTheOracle = pokeTheOracle;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
    }

    public async Task<bool> ProcessAsync(string fileName)
    {
        // The dream is for this, with the help of AI, to be able to process any log format and 
        // all other log handlers to be retired.  For now, just process voice memos.

        _logger.LogInformation("Processing generic log");

        try
        {
            bool success = true;

            var logEntry = await LoadGenericLogAsync(fileName);
            if (logEntry == null)
                success = false;
            else
            {
                var voiceMemo = await ParseVoiceMemoAsync(logEntry);
                if (voiceMemo == null)
                    success = false;
                else
                    success = await PostVoiceMemoAsync(voiceMemo);
            }

            // Not sure how many times this will occur, but currently I am still using paid
            // for model and I don't want to be eating up my tokens unnecesarilly..
            if (!success)
                RenameFileWithPrefix(fileName, "error-");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing generic log: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> PostVoiceMemoAsync(VoiceMemo voiceMemo)
    {
        bool success = true;

        try
        {
            VoiceLogDto voiceLogDto = new VoiceLogDto
            {
                SnapshotDateUTC = DateTime.ParseExact(voiceMemo.MemoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                ActivityName = voiceMemo.Name,
                Quantity = System.Convert.ToDecimal(voiceMemo.Quantity),
                UnitOfMeasure = voiceMemo.Uom
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonData = JsonSerializer.Serialize(voiceLogDto, jsonOptions);

            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_dashboardUrl + @"/activity/add-voice-memo", content);

            if (response.IsSuccessStatusCode)
            {
                success = true;
                _logger.LogInformation($"Successfully added voice memo {voiceLogDto.ActivityName}");
            }
            else
            {
                success = false;
                _logger.LogInformation($"Failed to add food price for {voiceLogDto.ActivityName})");
            }
        }
        catch (FormatException ex)
        {
            success = false;
            _logger.LogInformation($"Formatting exception occured when attempting to map voice memo {ex.Message})");
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogInformation($"Unexpected error processing voice memo: {ex.Message}");
        }

        return success;
    }

    public async Task<VoiceMemo> ParseVoiceMemoAsync(string logEntry)
    {
        var response = await _pokeTheOracle.InvokeKernelFunctionAsync("log", "ParseVoiceMemo", new Dictionary<string, string> { { "logContent", logEntry } });

        response = response.Replace("```json", "").Replace("```", "").Trim();
        response = response.Replace("\\n", "").Replace("\\\"", "\"");
        response = response.Replace("\\n", "").Replace("\\", "");

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            VoiceMemo voiceMemo = JsonSerializer.Deserialize<VoiceMemo>(response, options);
            return voiceMemo;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing voice memo: {ex.Message}");
            return null;
        }



        // Give the AI five chances to get a correct response.  
        // for (int i = 0; i < 5; i++)
        // {
        //     var response = await _pokeTheOracle.InvokeKernelFunctionAsync("log", "ParseVoiceMemo", new Dictionary<string, string> { { "logContent", logEntry } });

        //     var activityDatePattern = @"Activity date:\s*(?<date>.*)";
        //     var quantityPattern = @"Activity Quantity:\s*\$(?<quantity>[0-9,]+(\.\d{2})?)";

        //     var dateMatch = Regex.Match(response.ToString(), activityDatePattern);
        //     var quantityMatch = Regex.Match(response.ToString(), quantityPattern);

        //     if (dateMatch.Success || quantityMatch.Success)
        //     {
        //         logDate = DateTime.Parse(dateMatch.Groups["date"].Value.Trim());
        //         quantity = decimal.Parse(quantityMatch.Groups["quantity"].Value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        //         break;
        //     }
        // }

        // if (logDate == DateTime.MinValue || quantity == 0)
        //     throw new FormatException("Unable to parse the email content.");


    }

    private async Task<string> LoadGenericLogAsync(string fileName)
    {
        try
        {
            string fileContent = await File.ReadAllTextAsync(fileName);
            return fileContent;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading in generic log: {ex.Message}");
            return null;
        }
    }
    public void RenameFileWithPrefix(string filePath, string filePrefix)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(filePrefix))
            return;


        if (!File.Exists(filePath))
            return;

        string directory = Path.GetDirectoryName(filePath);
        string originalFileName = Path.GetFileName(filePath);
        string newFileName = filePrefix + originalFileName;
        string newFilePath = Path.Combine(directory, newFileName);

        if (File.Exists(newFilePath))
            return;

        File.Move(filePath, newFilePath);
        _logger.LogError($"Flagged as do not process: {newFileName}");
    }
}