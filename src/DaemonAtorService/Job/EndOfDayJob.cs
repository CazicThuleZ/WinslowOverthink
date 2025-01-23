using System;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Web;
using DaemonAtorService.Helper;
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
    private readonly PostgresBackupService _backupService;

    public EndOfDayJob(ILogger<EndOfDayJob> logger, IOptions<GlobalSettings> globalSettings, HttpClient httpClient, IOptions<List<DirectorySyncSettings>> syncSettings, PostgresBackupService backupService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
        _syncSettings = syncSettings.Value;
        _backupService = backupService;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"EndOfDayJob started at {DateTimeOffset.Now}");

        await UpdateFoodDietDiaryCosts();
        await SyncDirectories();
        await _backupService.PerformBackups();        

        _logger.LogInformation($"EndOfDayJob completed at {DateTimeOffset.Now}");

    }

    private async Task SyncDirectories()
    {
        int syncedFileCount = 0;
        foreach (var setting in _syncSettings)
        {
            try
            {
                if (string.IsNullOrEmpty(setting.SourceDirectory) || string.IsNullOrEmpty(setting.DestinationDirectory))
                {
                    _logger.LogError($"Invalid directory configuration: Source or destination path is empty");
                    continue;
                }

                if (!Directory.Exists(setting.SourceDirectory))
                {
                    _logger.LogError($"Source directory does not exist: {setting.SourceDirectory}");
                    continue;
                }

                Directory.CreateDirectory(setting.DestinationDirectory);

                var files = Directory.GetFiles(setting.SourceDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(setting.SourceDirectory, file);
                        var destFile = setting.MirrorDirectoryStructure
                            ? Path.Combine(setting.DestinationDirectory, relativePath)
                            : Path.Combine(setting.DestinationDirectory, Path.GetFileName(file));

                        var destDir = Path.GetDirectoryName(destFile);
                        if (!Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        var sourceInfo = new FileInfo(file);
                        var destInfo = new FileInfo(destFile);

                        if (!destInfo.Exists || sourceInfo.LastWriteTimeUtc > destInfo.LastWriteTimeUtc)
                        {
                            await RetryWithTimeout(() =>
                            {
                                File.Copy(file, destFile, true);
                                syncedFileCount++;
                                return Task.CompletedTask;
                            }, 3, TimeSpan.FromSeconds(30));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to sync file {file}: {ex.Message}");
                    }
                }

                _logger.LogInformation($"Directory synchronization completed for {setting.SourceDirectory} → {setting.DestinationDirectory}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Critical error during directory synchronization: {ex.Message}");
            }
        }

        _logger.LogInformation($"Total files synced: {syncedFileCount}");
    }

    private async Task RetryWithTimeout(Func<Task> action, int maxAttempts, TimeSpan timeout)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeout);
                await action().WaitAsync(cts.Token);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning($"Attempt {attempt} failed: {ex.Message}. Retrying...");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
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
