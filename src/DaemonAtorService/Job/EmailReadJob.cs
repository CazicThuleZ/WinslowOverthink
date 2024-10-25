using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Quartz;

namespace DaemonAtorService
{
    [DisallowConcurrentExecution]
    public class EmailReadJob : IJob
    {
        private readonly ILogger<EmailReadJob> _logger;
        private readonly PokeTheOracle _pokeTheOracle;
        private readonly string _emailSaveDirectory;
        public readonly string _dashboardLogLocation;
        public readonly string _attachmentSaveLocation;

        private readonly ILoggingStrategy _loggingStrategy;
        private readonly string _emailAddress;
        private readonly string _appPassword;
        private readonly string _imapServer;
        private readonly int _imapPort;

        public EmailReadJob(
            ILogger<EmailReadJob> logger,
            IOptions<GmailApiSettings> gmailApiSettings,
            IOptions<GlobalSettings> globalSettings,
            PokeTheOracle pokeTheOracle,
            ILoggingStrategy loggingStrategy)
        {
            _logger = logger;
            _emailSaveDirectory = gmailApiSettings.Value.EmailSaveDirectory;
            _dashboardLogLocation = globalSettings.Value.DashboardLogLocation;
            _attachmentSaveLocation = gmailApiSettings.Value.AttachmentSaveLocation;
            _pokeTheOracle = pokeTheOracle;
            _loggingStrategy = loggingStrategy;

            // Get email settings from configuration
            _emailAddress = gmailApiSettings.Value.EmailAddress;
            _appPassword = gmailApiSettings.Value.AppPassword;
            _imapServer = gmailApiSettings.Value.ImapServer ?? "imap.gmail.com";
            _imapPort = gmailApiSettings.Value.ImapPort != 0 ? gmailApiSettings.Value.ImapPort : 993;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("EmailReadJob started at {time}", DateTimeOffset.Now);

            try
            {
                using (var client = new ImapClient())
                {
                    await client.ConnectAsync(_imapServer, _imapPort, SecureSocketOptions.SslOnConnect);
                    await client.AuthenticateAsync(_emailAddress, _appPassword);

                    var inbox = client.Inbox;
                    await inbox.OpenAsync(FolderAccess.ReadWrite);

                    // Search for unread messages
                    var uids = await inbox.SearchAsync(SearchQuery.NotSeen);

                    if (uids.Count > 0)
                    {
                        _logger.LogInformation("Unread messages found in Inbox: {count}.", uids.Count);

                        foreach (var uid in uids)
                        {
                            var message = await inbox.GetMessageAsync(uid);
                            var subject = message.Subject;
                            var emailDate = message.Date.ToString("yyyyMMddHHmmss");

                            try
                            {
                                var handlerFactory = new EmailHandlerFactory(this);
                                var handler = handlerFactory.GetHandler(subject);

                                if (handler != null)
                                {
                                    await handler.HandleAsync(subject, message, emailDate, _loggingStrategy);
                                }

                                SaveEmailToFile(message, _emailSaveDirectory);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Failed to process email: {subject}. {exception}", subject, ex.Message);
                            }
                            finally
                            {
                                // Mark as read
                                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No unread messages found.");
                    }

                    await DeleteOldEmails(client);

                    await client.DisconnectAsync(true);
                }
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
            decimal accountBalance = 0;

            // Give the AI five chances to get a correct response.
            for (int i = 0; i < 5; i++)
            {
                var response = await _pokeTheOracle.InvokeKernelFunctionAsync(
                    "email",
                    "DeriveBalance",
                    new Dictionary<string, string> { { "emailBody", emailBody } });

                var sentDatePattern = @"Sent date:\s*(?<date>.*)";
                var balancePattern = @"Account balance:\s*\$?(?<balance>[0-9,]+(\.\d{2})?)";

                var dateMatch = Regex.Match(response.ToString(), sentDatePattern);
                var balanceMatch = Regex.Match(response.ToString(), balancePattern);

                if (dateMatch.Success && balanceMatch.Success)
                {
                    sentDate = DateTime.Parse(dateMatch.Groups["date"].Value.Trim());
                    accountBalance = decimal.Parse(balanceMatch.Groups["balance"].Value.Trim(), NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    break;
                }
            }

            if (sentDate == DateTime.MinValue)
                sentDate = DateTime.UtcNow;

            if (accountBalance == 0)
                throw new FormatException("Unable to parse the email content.");

            return (sentDate, accountBalance);
        }

        public async Task<LogAccountBalance> ParseAccountBalanceAlert(string subject, MimeMessage message, string emailDate)
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

        private void SaveEmailToFile(MimeMessage message, string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fileName = string.IsNullOrEmpty(message.MessageId) ? Guid.NewGuid().ToString() : message.MessageId;
            string filePath = Path.Combine(directory, $"{fileName}.txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"From: {message.From}");
                writer.WriteLine($"To: {message.To}");
                writer.WriteLine($"Subject: {message.Subject}");
                writer.WriteLine($"Date: {message.Date}");
                writer.WriteLine();
                string body = GetEmailBody(message);
                writer.WriteLine(body);
            }
        }

        private string GetEmailBody(MimeMessage message)
        {
            if (!string.IsNullOrEmpty(message.TextBody))
                return message.TextBody;
            else if (!string.IsNullOrEmpty(message.HtmlBody))
                return message.HtmlBody;
            else
                return "No readable body found.";
        }

        public async Task<LogScaleWeight> ParseLoseItSummary(string subject, MimeMessage message, string emailDate)
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

        public void SaveAttachment(string attachmentSaveLocation, MimeMessage message)
        {
            if (!Directory.Exists(attachmentSaveLocation))
                Directory.CreateDirectory(attachmentSaveLocation);

            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart mimePart && !string.IsNullOrEmpty(mimePart.FileName))
                {
                    var filePath = Path.Combine(attachmentSaveLocation, mimePart.FileName);
                    using (var stream = File.Create(filePath))
                    {
                        mimePart.Content.DecodeTo(stream);
                    }
                }
            }
        }

        private async Task DeleteOldEmails(ImapClient client)
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var thresholdDate = DateTime.UtcNow.AddDays(-30);
            var query = SearchQuery.DeliveredBefore(thresholdDate);
            var uids = await inbox.SearchAsync(query);

            if (uids.Count > 0)
            {
                foreach (var uid in uids)
                {
                    try
                    {
                        await inbox.AddFlagsAsync(uid, MessageFlags.Deleted, true);
                        _logger.LogInformation($"Deleted email with UID: {uid} as it is older than 30 days.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to delete email with UID: {uid}.");
                    }
                }

                await inbox.ExpungeAsync();
            }
        }

        private async Task<string> ExtractNumericValue(string input)
        {
            // Simulate async operation if needed
            await Task.CompletedTask;
            var match = Regex.Match(input, @"[\d.]+");
            return match.Success ? match.Value : "0.0";
        }
    }
}