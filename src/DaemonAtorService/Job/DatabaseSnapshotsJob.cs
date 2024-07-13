using System.Diagnostics;
using System.Text.Json;
using DaemonAtorService.DTOs;
using Microsoft.Extensions.Options;
using Quartz;

namespace DaemonAtorService;

[DisallowConcurrentExecution]
public class DatabaseSnapshotsJob : IJob
{
    private readonly ILogger<DatabaseSnapshotsJob> _logger;
    private readonly int _retainArchiveDays;
    private readonly string _dashboardLogLocation;
    private readonly string _archiveFileLocation;
    private readonly LogHandlerFactory _logHandlerFactory;
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;

    public DatabaseSnapshotsJob(ILogger<DatabaseSnapshotsJob> logger,
                                IOptions<ArchiveFileSettings> archiveFileSettings,
                                IOptions<GlobalSettings> globalSettings,
                                LogHandlerFactory logHandlerFactory,
                                HttpClient httpClient)
    {
        _logger = logger;
        _dashboardLogLocation = globalSettings.Value.DashboardLogLocation;
        _logHandlerFactory = logHandlerFactory;
        _retainArchiveDays = archiveFileSettings.Value.RetainArchiveDays;
        _archiveFileLocation = archiveFileSettings.Value.ArchiveFilePath;
        _httpClient = httpClient;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
    }

    public Task Execute(IJobExecutionContext context)
    {
        ExecuteAsync(context).GetAwaiter().GetResult();
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"DatabaseSnapshots started at {DateTimeOffset.Now}");

        try
        {
            var response = await _httpClient.GetAsync(_dashboardUrl + "/utility/health-check");
            var files = Directory.GetFiles(_dashboardLogLocation, "*.*", SearchOption.AllDirectories)
                                 .Where(file => !Path.GetFileName(file).StartsWith("error-", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();
                                 
            if (response.IsSuccessStatusCode && files.Length > 0)
            {
                foreach (var file in files)
                {
                    bool isSuccess = false;
                    try
                    {
                        var format = DetectLogFormat(file);
                        var processor = _logHandlerFactory.GetFileProcessor(format);
                        isSuccess = await processor.ProcessAsync(file);
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        _logger.LogError($"DatabaseSnapshotsJob encountered an error: {ex.Message}");
                    }
                    if (isSuccess)
                    {
                        string archivePath = Path.Combine(_archiveFileLocation, DateTime.Now.ToString("yyyyMMdd"));
                        if (!Directory.Exists(archivePath))
                            Directory.CreateDirectory(archivePath);

                        string archiveFile = Path.Combine(archivePath, Path.GetFileName(file));
                        File.Move(file, archiveFile);
                        _logger.LogInformation($"File {file} moved to {archiveFile}");
                    }
                }

                PurgeArchives();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard service not available or no files to process.");
        }

        _logger.LogInformation($"DatabaseSnapshots completed at {DateTimeOffset.Now}");

    }
    private LogFileFormat DetectLogFormat(string filePath)
    {
        var firstLine = File.ReadLines(filePath).FirstOrDefault();

        if (firstLine == null)
        {
            return LogFileFormat.Other;
        }
        else
        {
            if (firstLine.Contains("Calories", StringComparison.OrdinalIgnoreCase) &&
                firstLine.Contains("Fat", StringComparison.OrdinalIgnoreCase) &&
                firstLine.Contains("Protein", StringComparison.OrdinalIgnoreCase))
                return LogFileFormat.LoseItDailySummary;

            else if (firstLine.Contains("Weight", StringComparison.OrdinalIgnoreCase))
                return LogFileFormat.DietScale;
            else if (firstLine.Contains("Symbol", StringComparison.OrdinalIgnoreCase))
                return LogFileFormat.CryptoPrice;
            else if (firstLine.Contains("Account balance as of", StringComparison.OrdinalIgnoreCase))
                return LogFileFormat.BalanceAlerts;
            else if (firstLine.Contains("AccountName", StringComparison.OrdinalIgnoreCase))
                return LogFileFormat.BalanceAlerts;
            else if (firstLine.Contains("Model", StringComparison.OrdinalIgnoreCase))
                return LogFileFormat.TokenUsage;
            else
                return LogFileFormat.Other;

        }
    }
    private void PurgeArchives()
    {
        string archiveRootPath = _archiveFileLocation;

        if (!Directory.Exists(archiveRootPath))
            return;

        var files = Directory.GetFiles(archiveRootPath, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-_retainArchiveDays))
            {
                try
                {
                    File.Delete(file);
                    _logger.LogInformation($"Deleted old archive file: {file}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete file: {file}");
                }
            }
        }
    }
}
