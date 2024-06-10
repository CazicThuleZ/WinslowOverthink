using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace DaemonAtorService;

public class FidelityHandler : IEmailHandler
{
    private readonly EmailReadJob _job;
    public FidelityHandler(EmailReadJob job)
    {
        _job = job;
    }
    public async Task<string> HandleAsync(string subject, Message message, string emailDate, GmailService service)
    {
        var logMessage = await _job.ParseAccountBalanceAlert(subject, message, emailDate);
        if (!string.IsNullOrEmpty(logMessage))
        {
            _job.LogForDashboard("Health Savings " + logMessage, subject, _job._dashboardLogLocation, _job._attachmentSaveLocation, "FidelityHealth", message, service);
        }
        return logMessage;
    }
}