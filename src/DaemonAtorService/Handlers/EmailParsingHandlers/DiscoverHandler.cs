using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace DaemonAtorService;

public class DiscoverHandler : IEmailHandler
{
    private readonly EmailReadJob _job;

    public DiscoverHandler(EmailReadJob job)
    {
        _job = job;
    }
    public async Task<string> HandleAsync(string subject, Message message, string emailDate, GmailService service)
    {
        var logMessage = await _job.ParseAccountBalanceAlert(subject, message, emailDate);
        if (!string.IsNullOrEmpty(logMessage))
        {
            _job.LogForDashboard("Discover Savings " + logMessage, subject, _job._dashboardLogLocation, _job._attachmentSaveLocation, "DiscoverSavings", message, service);
        }
        return logMessage;
    }
}
