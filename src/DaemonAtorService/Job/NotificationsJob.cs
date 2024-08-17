using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DaemonAtorService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace DaemonAtorService
{
    [DisallowConcurrentExecution]
    public class NotificationJob : IJob
    {
        private readonly ILogger<NotificationJob> _logger;
        private readonly string _logDirectoryPath;
        private readonly string _alertDirectoryPath;

        private readonly string _notifyAlertsPath;

        public NotificationJob(ILogger<NotificationJob> logger, IOptions<GlobalSettings> globalSettings)
        {
            _logger = logger;
            _logDirectoryPath = globalSettings.Value.ServiceLogsPath;
            _alertDirectoryPath = Path.Combine(_logDirectoryPath, "alerts");
            _notifyAlertsPath = globalSettings.Value.NotifyAlertsPath;

            Directory.CreateDirectory(_alertDirectoryPath);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Notify Job started at: {time}", DateTimeOffset.Now);

            ParseLogFiles();

            // TODO: Implement other notification types
            // BankBalanceWarning();
            // UnexpectedCryptoPriceRise();
            // IntruderAlert)();

            await Task.CompletedTask;
        }

        private void ParseLogFiles()
        {
            try
            {
                if (Directory.Exists(_logDirectoryPath))
                {
                    var logFiles = Directory.GetFiles(_logDirectoryPath, "Errors*.log");

                    foreach (var logFilePath in logFiles)
                    {
                        using (var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream))
                        {
                            var logLines = reader.ReadToEnd().Split(Environment.NewLine).ToList();

                            foreach (var line in logLines)
                            {
                                try
                                {
                                    var logEntry = JsonSerializer.Deserialize<LogEntry>(line);
                                    string logEntryHash = ComputeHash(line);
                                    string alertFilePath = Path.Combine(_alertDirectoryPath, $"{logEntryHash}.json");

                                    if (!File.Exists(alertFilePath))  // We may be reading this file often until its purged.  Only notify once per incident.
                                    {
                                        CreateNotification(alertFilePath, logEntry.Message + " : " + logEntry.ExceptionMsg1 + " : " + logEntry.ExceptionMsg2);
                                        File.WriteAllText(alertFilePath, line); // Send to WinslowNotify
                                    }
                                }
                                catch (JsonException)  // Catch specific JSON exceptions
                                {
                                    // I think safe to swallow these.  
                                }                                
                                catch (Exception ex)
                                {
                                    // But don't want to have this logic alerting on itself.
                                    _logger.LogInformation(ex, "Error processing log line in NotificationJob");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Notify Job");
            }
        }

        private void CreateNotification(string title, string message)
        {
            try
            {
                NotificationData notificationData = new NotificationData
                {
                    Notification = new NotificationInfo
                    {
                        Title = title,
                        Message = message
                    }
                };

                string jsonContent = JsonSerializer.Serialize(notificationData);

                string fileName = $"{Guid.NewGuid()}.json";
                string tempFilePath = Path.Combine(_notifyAlertsPath, $"{fileName}.tmp");
                string finalFilePath = Path.Combine(_notifyAlertsPath, fileName);

                // Write to a temporary file first to avoid locking issues, then rename.
                File.WriteAllText(tempFilePath, jsonContent);
                Thread.Sleep(100);
                File.Move(tempFilePath, finalFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification in NotificationJob");
            }
        }

        private string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private class LogEntry
        {
            [JsonPropertyName("@t")]
            public DateTimeOffset Time { get; set; }

            [JsonPropertyName("@mt")]
            public string Message { get; set; }

            [JsonPropertyName("@l")]
            public string LogType { get; set; }

            [JsonPropertyName("exception")]
            public string ExceptionMsg1 { get; set; }

            [JsonPropertyName("@x")]
            public string ExceptionMsg2 { get; set; }

            [JsonPropertyName("SourceContext")]
            public string SourceContext { get; set; }

            [JsonPropertyName("Application")]
            public string Application { get; set; }
        }
    }
}
