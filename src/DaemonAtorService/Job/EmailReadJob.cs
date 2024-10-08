﻿using System.Diagnostics;
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
    private readonly PokeTheOracle _pokeTheOracle;
    private readonly string _emailSaveDirectory;
    public readonly string _dashboardLogLocation;
    public readonly string _attachmentSaveLocation;

    private readonly ILoggingStrategy _loggingStrategy;

    public EmailReadJob(ILogger<EmailReadJob> logger, GmailServiceHelper gmailServiceHelper, IOptions<GmailApiSettings> gmailApiSettings, IOptions<GlobalSettings> globalSettings, PokeTheOracle pokeTheOracle, ILoggingStrategy loggingStrategy)
    {
        _logger = logger;
        _gmailServiceHelper = gmailServiceHelper;
        _emailSaveDirectory = gmailApiSettings.Value.EmailSaveDirectory;
        _dashboardLogLocation = globalSettings.Value.DashboardLogLocation;
        _attachmentSaveLocation = gmailApiSettings.Value.AttachmentSaveLocation;
        _pokeTheOracle = pokeTheOracle;
        _loggingStrategy = loggingStrategy;
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
                        var handlerFactory = new EmailHandlerFactory(this);
                        var handler = handlerFactory.GetHandler(subject);

                        if (handler != null)
                        {
                            await handler.HandleAsync(subject, message, emailDate, service, _loggingStrategy);
                            if (!string.IsNullOrEmpty(logMessage))
                                _logger.LogInformation(logMessage);
                        }

                        SaveEmailToFile(message, _emailSaveDirectory);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to process email: {subject}. {exception}", subject, ex.Message);
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

        // Give the AI five chances to get a correct response.  The temp is set to 0.0, so it should be precise.
        for (int i = 0; i < 5; i++)
        {
            var response = await _pokeTheOracle.InvokeKernelFunctionAsync("email", "DeriveBalance", new Dictionary<string, string> { { "emailBody", emailBody } });

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

    public async Task<LogAccountBalance> ParseAccountBalanceAlert(string subject, Message message, string emailDate)
    {
        string emailBody = GetEmailBody(message);

        var (sentDate, accountBalance) = await ParseAccountBalancesAsync(emailBody);

        LogAccountBalance logAccountBalance = new LogAccountBalance()
        {
            SnapshotDateUTC = sentDate,
            Balance = accountBalance
        };

        return logAccountBalance;

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

    public async Task<LogScaleWeight> ParseLoseItSummary(string subject, Message message, string emailDate)
    {
        if (!subject.Contains("Lose It!", StringComparison.OrdinalIgnoreCase))
            return new LogScaleWeight();

        string body = GetEmailBody(message);
        if (string.IsNullOrEmpty(body))
            return new LogScaleWeight();

        string returnWeight = "0.0";
        try
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(body);

            var weightNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), \"Today's Weight\")]/following-sibling::td");
            if (weightNode != null)
                returnWeight = await ExtractNumericValue(weightNode.InnerText.Trim());

            if (!decimal.TryParse(returnWeight, out decimal weight))
                throw new FormatException("Failed to parse weight.");

            if (!DateTime.TryParseExact(emailDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime snapshotDate))
                throw new FormatException("Failed to parse email date.");

            return new LogScaleWeight
            {
                SnapshotDateUTC = snapshotDate,
                Weight = weight
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Lose It! summary email.");
            return new LogScaleWeight();
        }
    }
    public void SaveAttachment(string attachmentSaveLocation, Message message, GmailService service)
    {

        if (!Directory.Exists(attachmentSaveLocation))
            Directory.CreateDirectory(attachmentSaveLocation);

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
    private async Task<string> ExtractNumericValue(string input)
    {
        // TODO add actual async operators
        await Task.Delay(1);
        var match = Regex.Match(input, @"[\d.]+");
        return match.Success ? match.Value : "0.0";
    }
}
