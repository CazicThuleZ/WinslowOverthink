using System.Diagnostics;
using System.Text.Json;
using DaemonAtorService.DTOs;
using Microsoft.Extensions.Options;
using Quartz;

namespace DaemonAtorService
{
    [DisallowConcurrentExecution]
    public class CalorieIntakeJob : IJob
    {
        private readonly ILogger<CalorieIntakeJob> _logger;
        private readonly string _csvFilePath;
        private readonly int _retainArchiveDays;
        private readonly string _dashboardLogLocation;

        public CalorieIntakeJob(ILogger<CalorieIntakeJob> logger, IOptions<CsvFileSettings> csvFileSettings, IOptions<GlobalSettings> globalSettings)
        {
            _logger = logger;
            _csvFilePath = csvFileSettings.Value.CsvFilePath;
            _retainArchiveDays = csvFileSettings.Value.RetainArchiveDays;
            _dashboardLogLocation = globalSettings.Value.DashboardLogLocation;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"CalorieIntakeJob started at {DateTimeOffset.Now}");

            try
            {
                var files = Directory.GetFiles(_csvFilePath, "Daily Report*.csv");

                foreach (var file in files)
                {
                    CsvParser parser = new CsvParser();
                    var dietStatistics = parser.ParseCsv(file);
                    var isSuccess = LogDietData(file, dietStatistics);

                    if (isSuccess)
                    {
                        string archivePath = Path.Combine(_csvFilePath, "Archive");
                        if (!Directory.Exists(archivePath))
                            Directory.CreateDirectory(archivePath);

                        string archiveFile = Path.Combine(archivePath, Path.GetFileName(file));
                        File.Move(file, archiveFile);
                        _logger.LogInformation($"File {file} moved to {archiveFile}");
                    }
                }

                PurgeArchives();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CalorieIntakeJob encountered an error: {ex.Message}");
            }

            _logger.LogInformation($"CalorieIntakeJob completed at {DateTimeOffset.Now}");
            return Task.CompletedTask;
        }

        private bool LogDietData(string fileName, List<DietStatistic> dietStatistics)
        {
            try
            {
                if (dietStatistics == null || !dietStatistics.Any())
                {
                    _logger.LogInformation($"No data to log for file: {fileName}");
                    return false;
                }

                var reportDate = dietStatistics.First().Date;                

                var dietStatDto = new DietStatDto
                {
                    SnapshotDateUTC = reportDate,
                    Calories = dietStatistics.Sum(stat => stat.Calories),
                    FatGrams = dietStatistics.Sum(stat => stat.Fat),
                    ProteinGrams = dietStatistics.Sum(stat => stat.Protein),
                    CarbGrams = dietStatistics.Sum(stat => stat.Carbohydrates),
                    SaturatedFatGrams = dietStatistics.Sum(stat => stat.SaturatedFat),
                    SugarGrams = dietStatistics.Sum(stat => stat.Sugars),
                    FiberGrams = dietStatistics.Sum(stat => stat.Fiber),
                    CholesterolGrams = dietStatistics.Sum(stat => stat.Cholesterol),
                    SodiumMilliGrams = dietStatistics.Sum(stat => stat.Sodium),
                    ExerciseCalories = dietStatistics
                        .Where(stat => stat.Calories < 0 )
                        .Sum(stat => stat.Calories)
                };

                string calorieReportPath = Path.Combine(_dashboardLogLocation, "CalorieReport");
                if (!Directory.Exists(calorieReportPath))
                    Directory.CreateDirectory(calorieReportPath);

                string logFileName = $"DietStats-{DateTime.Now:yyyyMMddHHmmss}.json";
                string logFilePath = Path.Combine(calorieReportPath, logFileName);

                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonData = JsonSerializer.Serialize(dietStatDto, jsonOptions);

                File.WriteAllText(logFilePath, jsonData);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to log diet data for file: {fileName}");
                return false;
            }
        }

        private void PurgeArchives()
        {
            string archivePath = Path.Combine(_csvFilePath, "Archive");

            if (!Directory.Exists(archivePath))
                return;

            var files = Directory.GetFiles(archivePath);

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
}
