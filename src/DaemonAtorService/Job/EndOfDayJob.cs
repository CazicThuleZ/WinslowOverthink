using System;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Options;
using Quartz;

namespace DaemonAtorService;

[DisallowConcurrentExecution]
public class EndOfDayJob : IJob
{
    private readonly ILogger<EndOfDayJob> _logger;
    private readonly List<DirectorySyncSettings> _syncSettings;
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;

    public EndOfDayJob(ILogger<EndOfDayJob> logger, IOptions<GlobalSettings> globalSettings, HttpClient httpClient, IOptions<List<DirectorySyncSettings>> syncSettings)
    {
        _logger = logger;
        _httpClient = httpClient;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
        _syncSettings = syncSettings.Value;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"EndOfDayJob started at {DateTimeOffset.Now}");

        await UpdateFoodDietDiaryCosts();
        await SyncDirectories();

        _logger.LogInformation($"EndOfDayJob completed at {DateTimeOffset.Now}");

    }

    private async Task SyncDirectories()
    {
        int syncedFileCount = 0;
        foreach (var setting in _syncSettings)
        {
            try
            {
                string sourceDirectory = setting.SourceDirectory;
                string destinationDirectory = setting.DestinationDirectory;
                bool mirrorStructure = setting.MirrorDirectoryStructure;

                if (!Directory.Exists(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, file);
                    var destFile = mirrorStructure ? Path.Combine(destinationDirectory, relativePath) : Path.Combine(destinationDirectory, Path.GetFileName(file));

                    var destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    if (!File.Exists(destFile))
                    {
                        File.Copy(file, destFile, false);
                        syncedFileCount++;
                    }
                }

                _logger.LogInformation($"Directory synchronization completed successfully for source: {sourceDirectory} to destination: {destinationDirectory}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during directory synchronization for source: {setting.SourceDirectory} to destination: {setting.DestinationDirectory}. Error: {ex.Message}");
            }
        }

        _logger.LogInformation($"EndOfDayJob synced files: {syncedFileCount.ToString()}");

        await Task.CompletedTask;

    }

    public async Task UpdateFoodDietDiaryCosts()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PutAsync(_dashboardUrl + @"/diet/update-diet-cost", null);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Updated Meal Log Costs {result}");
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Failed to update Meal Log Costs {errorMessage}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP Exception: {ex.Message}");

        }
        catch (Exception ex)
        {
            _logger.LogError($"Request error: {ex.Message}");
        }
    }
}
