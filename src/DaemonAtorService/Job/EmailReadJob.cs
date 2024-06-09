using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Quartz;

namespace DaemonAtorService;


[DisallowConcurrentExecution]
public class EmailReadJob : IJob
{
    private readonly ILogger<EmailReadJob> _logger;
    private readonly GmailServiceHelper _gmailServiceHelper;
    private readonly string _emailSaveDirectory;
    private readonly string _dashboardLogLocation;
    private readonly string _semanticKernelPluginLocation;
    private readonly string _attachmentSaveLocation;
    private readonly Kernel _kernel;

    public EmailReadJob(ILogger<EmailReadJob> logger, GmailServiceHelper gmailServiceHelper, IOptions<GmailApiSettings> gmailApiSettings, IOptions<GlobalSettings> globalSettings, Kernel kernel)
    {
        _logger = logger;
        _gmailServiceHelper = gmailServiceHelper;
        _emailSaveDirectory = gmailApiSettings.Value.EmailSaveDirectory;
        _dashboardLogLocation = globalSettings.Value.DashboardLogLocation;
        _semanticKernelPluginLocation = globalSettings.Value.SemanticKernelPluginsPath;
        _attachmentSaveLocation = gmailApiSettings.Value.AttachmentSaveLocation;
        _kernel = kernel;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("EmailReadJob started at {time}", DateTimeOffset.Now);

        try
        {
            var credential = await _gmailServiceHelper.GetUserCredentialAsync();
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = GmailServiceHelper.ApplicationName,
            });

            // Define parameters of request.
            UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
            request.Q = "is:unread";
            request.MaxResults = 100;

            // List messages with pagination
            List<Message> allMessages = new List<Message>();
            ListMessagesResponse response = null;

            do
            {
                response = await request.ExecuteAsync();
                if (response.Messages != null)
                {
                    allMessages.AddRange(response.Messages);
                }
                request.PageToken = response.NextPageToken;
            } while (!string.IsNullOrEmpty(response.NextPageToken));

            if (allMessages.Count > 0)
            {
                _logger.LogInformation("Unread messages found in Inbox: {allMessages.Count}.", allMessages.Count.ToString());
                foreach (var messageItem in allMessages)
                {
                    var message = await service.Users.Messages.Get("me", messageItem.Id).ExecuteAsync();
                    var subject = GetHeader(message, "Subject");
                    var emailDate = GetHeader(message, "Date");

                    try
                    {
                        if (DateTime.TryParseExact(emailDate, "ddd, dd MMM yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedDate))
                            emailDate = parsedDate.ToString("yyyyMMddHHmmss");
                        else if (DateTime.TryParseExact(emailDate, "ddd, dd MMM yyyy HH:mm:ss zzz '(UTC)'", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedDate2))
                            emailDate = parsedDate2.ToString("yyyyMMddHHmmss");
                        else if (DateTime.TryParseExact(emailDate, "ddd, dd MMM yyyy HH:mm:ss zzz GMT", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedDate3))
                            emailDate = parsedDate3.ToString("yyyyMMddHHmmss");
                        else
                            throw new FormatException("Unable to parse the date.");

                        var logMessage = string.Empty;
                        switch (subject)
                        {
                            case var s when s.Contains("Lookout CU balance alert", StringComparison.OrdinalIgnoreCase):
                                logMessage = await ParseAccountBalanceAlert(subject, message, emailDate);
                                if (!string.IsNullOrEmpty(logMessage))
                                    LogForDashboard("Checking Account " + logMessage, subject, _dashboardLogLocation, _attachmentSaveLocation, "CUChecking", message, service);
                                break;
                            case var s when s.Contains("Lose It! Daily Summary", StringComparison.OrdinalIgnoreCase):
                                logMessage = ParseLoseItSummary(subject, message, emailDate);
                                if (!string.IsNullOrEmpty(logMessage))
                                    LogForDashboard(logMessage, subject, _dashboardLogLocation, _attachmentSaveLocation, "CalorieReport", message, service);
                                break;
                            case var s when s.Contains("Fidelity Alerts", StringComparison.OrdinalIgnoreCase):
                                logMessage = await ParseAccountBalanceAlert(subject, message, emailDate);
                                if (!string.IsNullOrEmpty(logMessage))
                                    LogForDashboard("Health Savings " + logMessage, subject, _dashboardLogLocation, _attachmentSaveLocation, "FidelityHealth", message, service);
                                break;
                            case var s when s.Contains("Current Balance Alert", StringComparison.OrdinalIgnoreCase):
                                logMessage = await ParseAccountBalanceAlert(subject, message, emailDate);
                                if (!string.IsNullOrEmpty(logMessage))
                                    LogForDashboard("Discover Savings " + logMessage, subject, _dashboardLogLocation, _attachmentSaveLocation, "DiscoverSavings", message, service);
                                break;
                        }

                        SaveEmailToFile(message, _emailSaveDirectory);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Failed to process email: {subject}. {exception}", subject, ex.Message);
                    }
                    finally
                    {
                        MarkAsRead(service, messageItem.Id);
                    }
                }
            }
            else
            {
                _logger.LogInformation("No unread messages found.");
            }

            await DeleteOldEmails(service);
        }
        catch (Exception ex)
        {
            _logger.LogError("EmailReadJob encountered an error: {exception}", ex.Message);
        }

        _logger.LogInformation("EmailReadJob completed at {time}", DateTimeOffset.Now);
    }

    public async Task<(DateTime SentDate, decimal Balance)> ParseAccountBalancesAsync(string emailBody)
    {

        DateTime sentDate = DateTime.MinValue;
        Decimal accountBalance = 0;

        var emailPluginsPath = Path.Combine(_semanticKernelPluginLocation, "InterpretEmails");
        var emailPluginsFunction = _kernel.ImportPluginFromPromptDirectory(emailPluginsPath);
        var arguments = new KernelArguments() { { "emailBody", emailBody } };

        // Give the AI five chances to get a correct response.  The temp is set to 0.0, so it should be precise.
        for (int i = 0; i < 5; i++)
        {
            var response = await _kernel.InvokeAsync(emailPluginsFunction["DeriveBalance"], arguments);

            var sentDatePattern = @"Sent date:\s*(?<date>.*)";
            var balancePattern = @"Account balance:\s*\$(?<balance>[0-9,]+(\.\d{2})?)";

            var dateMatch = Regex.Match(response.ToString(), sentDatePattern);
            var balanceMatch = Regex.Match(response.ToString(), balancePattern);

            if (dateMatch.Success || balanceMatch.Success)
            {
                sentDate = DateTime.Parse(dateMatch.Groups["date"].Value.Trim());
                accountBalance = decimal.Parse(balanceMatch.Groups["balance"].Value.Trim(), NumberStyles.Currency, CultureInfo.InvariantCulture);
                break;
            }
        }

        if (sentDate == DateTime.MinValue || accountBalance == 0)
            throw new FormatException("Unable to parse the email content.");

        return (sentDate, accountBalance);
    }

    public async Task<string> ParseAccountBalanceAlert(string subject, Message message, string emailDate)
    {
        string emailBody = GetEmailBody(message);

        var (sentDate, accountBalance) = await ParseAccountBalancesAsync(emailBody);

        return $"Account balance as of {sentDate:yyyy-MM-dd HH:mm:ss} is: {accountBalance:C}";

    }
    private void SaveEmailToFile(Message message, string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string filePath = Path.Combine(directory, $"{message.Id}.txt");
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine($"From: {GetHeader(message, "From")}");
            writer.WriteLine($"To: {GetHeader(message, "To")}");
            writer.WriteLine($"Subject: {GetHeader(message, "Subject")}");
            writer.WriteLine($"Date: {GetHeader(message, "Date")}");
            writer.WriteLine();
            string body = GetEmailBody(message);
            writer.WriteLine(body);
        }
    }
    private string GetHeader(Google.Apis.Gmail.v1.Data.Message message, string headerName)
    {
        var header = message.Payload.Headers.FirstOrDefault(h => h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        return header != null ? header.Value : "N/A";
    }

    private string GetEmailBody(Message message)
    {
        if (message.Payload.Parts == null || message.Payload.Parts.Count == 0)
            return DecodeBase64String(message.Payload.Body.Data);

        return GetEmailBodyFromParts(message.Payload.Parts);
    }
    private string GetEmailBodyFromParts(IList<MessagePart> parts)
    {
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part.MimeType))
            {
                if (part.MimeType == "text/plain" && part.Body.Data != null)
                    return DecodeBase64String(part.Body.Data);
                else if (part.MimeType == "text/html" && part.Body.Data != null)
                    return DecodeBase64String(part.Body.Data);
                else if (part.MimeType == "multipart/alternative" || part.MimeType == "multipart/mixed" || part.MimeType == "multipart/related")
                {
                    if (part.Parts != null && part.Parts.Count > 0)
                    {
                        var result = GetEmailBodyFromParts(part.Parts);
                        if (!string.IsNullOrEmpty(result))
                            return result;
                    }
                }
            }
        }

        return "No readable body found.";
    }

    private string DecodeBase64String(string base64String)
    {
        var data = Convert.FromBase64String(base64String.Replace("-", "+").Replace("_", "/"));
        return System.Text.Encoding.UTF8.GetString(data);
    }

    private void MarkAsRead(GmailService service, string messageId)
    {
        var modifyRequest = new ModifyMessageRequest
        {
            RemoveLabelIds = new[] { "UNREAD" }
        };

        service.Users.Messages.Modify(modifyRequest, "me", messageId).Execute();
    }

    public string ParseLoseItSummary(string subject, Message message, string emailDate)
    {
        string returnWeight = "Not Available";
        if (!subject.Contains("Lose It!", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        string body = GetEmailBody(message);
        if (string.IsNullOrEmpty(body))
            return "No body content found.";

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(body);

        var weightNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), \"Today's Weight\")]/following-sibling::td");
        if (weightNode != null)
            returnWeight = weightNode.InnerText.Trim();

        return $"Weight as of {emailDate} is: {returnWeight}";
    }
    public void LogForDashboard(string text, string subject, string logLocation, string attachmentSaveLocation, string subDirectory, Message message, GmailService service)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(logLocation) || string.IsNullOrWhiteSpace(attachmentSaveLocation))
            throw new ArgumentException("Text, subject, log location, and attachment save location must be provided.");

        string subDirectoryPath = Path.Combine(logLocation, subDirectory);

        if (!Directory.Exists(subDirectoryPath))
            Directory.CreateDirectory(subDirectoryPath);

        if (!Directory.Exists(attachmentSaveLocation))
            Directory.CreateDirectory(attachmentSaveLocation);

        string date = DateTime.Now.ToString("yyyyMMddHHmmss");  // Uses 24-hour format
        string sanitizedSubject = string.Join("_", subject.Split(Path.GetInvalidFileNameChars()));  // Sanitize subject to be a valid file name
        string logFileName = $"{date}-{sanitizedSubject}.txt";
        string logFilePath = Path.Combine(subDirectoryPath, logFileName);

        using (StreamWriter writer = new StreamWriter(logFilePath, false))  // Overwrite if exists
        {
            writer.WriteLine($"{DateTime.Now}: {text}");
        }

        // Save attachments
        if (message.Payload.Parts != null && message.Payload.Parts.Count > 0)
        {
            foreach (var part in message.Payload.Parts)
            {
                if (!string.IsNullOrEmpty(part.Filename))
                {
                    string attachmentId = part.Body.AttachmentId;
                    var attachment = service.Users.Messages.Attachments.Get("me", message.Id, attachmentId).Execute();
                    byte[] attachmentData = Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/'));

                    string attachmentFilePath = Path.Combine(attachmentSaveLocation, part.Filename);
                    File.WriteAllBytes(attachmentFilePath, attachmentData);
                }
            }
        }
    }

    private async Task DeleteOldEmails(GmailService service)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy/MM/dd");

        UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
        request.Q = $"label:inbox before:{thresholdDate}";

        List<Google.Apis.Gmail.v1.Data.Message> allMessages = new List<Google.Apis.Gmail.v1.Data.Message>();
        ListMessagesResponse response = null;

        do
        {
            response = await request.ExecuteAsync();
            if (response.Messages != null)
            {
                allMessages.AddRange(response.Messages);
            }
            request.PageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(response.NextPageToken));

        if (allMessages.Count > 0)
        {
            foreach (var messageItem in allMessages)
            {
                try
                {
                    // Google will automatically delete the email from the trash after 30 days
                    var modifyRequest = new ModifyMessageRequest
                    {
                        AddLabelIds = new[] { "TRASH" }
                    };
                    service.Users.Messages.Modify(modifyRequest, "me", messageItem.Id).Execute();
                    _logger.LogInformation($"Moved email with ID: {messageItem.Id} to trash as it is older than 30 days.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to move email with ID: {messageItem.Id} to trash.");
                }
            }
        }
    }


}
