using System.Diagnostics;
using Microsoft.Extensions.Options;
using Quartz;

namespace DaemonAtorService;


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

        _logger.LogInformation("CalorieIntakeJob started at {time}", DateTimeOffset.Now);

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
                    _logger.LogInformation("File {file} moved to {archiveFile}", file, archiveFile);
                }
            }

            PurgeArchives();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CalorieIntakeJob encountered an error");
        }

        _logger.LogInformation("CalorieIntakeJob completed at {time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
    // private bool LogDietData(string fileName, List<DietStatistic> dietStatistics)
    // {
    //     _logger.LogInformation("Processing file: {fileName}", fileName);

    //     foreach (var stat in dietStatistics)
    //     {
    //         _logger.LogInformation("Date: {Date}, Name: {Name}, Type: {Type}, Quantity: {Quantity}, Units: {Units}, Calories: {Calories}, Fat: {Fat}, Protein: {Protein}, Carbohydrates: {Carbohydrates}, Saturated Fat: {SaturatedFat}, Sugars: {Sugars}, Fiber: {Fiber}, Cholesterol: {Cholesterol}, Sodium: {Sodium}",
    //             stat.Date, stat.Name, stat.Type, stat.Quantity, stat.Units, stat.Calories, stat.Fat, stat.Protein, stat.Carbohydrates, stat.SaturatedFat, stat.Sugars, stat.Fiber, stat.Cholesterol, stat.Sodium);
    //     }

    //     return true;
    // }

    private void LogStat(string statName, double statValue, DateTime reportDate, string calorieReportPath)
    {
        if (statValue == 0)
            return;

        string logMessage = $"{statName.Replace("Consumed", "")} consumption for date {reportDate:yyyyMMdd000000} is: {statValue}";

        string logFileName = $"{statName}-{DateTime.Now:yyyyMMddHHmmss}.txt";
        string logFilePath = Path.Combine(calorieReportPath, logFileName);

        using (StreamWriter writer = new StreamWriter(logFilePath, false))
        {
            writer.WriteLine(logMessage);
        }

        _logger.LogInformation("Logged {statName} data to {logFilePath}", statName, logFilePath);
    }


    private bool LogDietData(string fileName, List<DietStatistic> dietStatistics)
    {

        //     foreach (var stat in dietStatistics)
        //     {
        //         _logger.LogInformation("Date: {Date}, Name: {Name}, Type: {Type}, Quantity: {Quantity}, Units: {Units}, Calories: {Calories}, Fat: {Fat}, Protein: {Protein}, Carbohydrates: {Carbohydrates}, Saturated Fat: {SaturatedFat}, Sugars: {Sugars}, Fiber: {Fiber}, Cholesterol: {Cholesterol}, Sodium: {Sodium}",
        //             stat.Date, stat.Name, stat.Type, stat.Quantity, stat.Units, stat.Calories, stat.Fat, stat.Protein, stat.Carbohydrates, stat.SaturatedFat, stat.Sugars, stat.Fiber, stat.Cholesterol, stat.Sodium);
        //     }

        try
        {
            if (dietStatistics == null || !dietStatistics.Any())
            {
                _logger.LogInformation("No data to log for file: {fileName}", fileName);
                return false;
            }

            var reportDate = dietStatistics.First().Date;

            var totalCalories = dietStatistics.Sum(stat => stat.Calories);
            var totalFat = dietStatistics.Sum(stat => stat.Fat);
            var totalProtein = dietStatistics.Sum(stat => stat.Protein);
            var totalCarbohydrates = dietStatistics.Sum(stat => stat.Carbohydrates);
            var totalSaturatedFat = dietStatistics.Sum(stat => stat.SaturatedFat);
            var totalSugars = dietStatistics.Sum(stat => stat.Sugars);
            var totalFiber = dietStatistics.Sum(stat => stat.Fiber);
            var totalCholesterol = dietStatistics.Sum(stat => stat.Cholesterol);
            var totalSodium = dietStatistics.Sum(stat => stat.Sodium);

            string calorieReportPath = Path.Combine(_dashboardLogLocation, "CalorieReport");
            if (!Directory.Exists(calorieReportPath))
                Directory.CreateDirectory(calorieReportPath);

            LogStat("CaloriesConsumed", totalCalories, reportDate, calorieReportPath);
            LogStat("FatConsumed", totalFat, reportDate, calorieReportPath);
            LogStat("ProteinConsumed", totalProtein, reportDate, calorieReportPath);
            LogStat("CarbohydratesConsumed", totalCarbohydrates, reportDate, calorieReportPath);
            LogStat("SaturatedFatConsumed", totalSaturatedFat, reportDate, calorieReportPath);
            LogStat("SugarsConsumed", totalSugars, reportDate, calorieReportPath);
            LogStat("FiberConsumed", totalFiber, reportDate, calorieReportPath);
            LogStat("CholesterolConsumed", totalCholesterol, reportDate, calorieReportPath);
            LogStat("SodiumConsumed", totalSodium, reportDate, calorieReportPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log diet data for file: {fileName}", fileName);
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
                    _logger.LogInformation("Deleted old archive file: {file}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file: {file}", file);
                }
            }
        }
    }
}
