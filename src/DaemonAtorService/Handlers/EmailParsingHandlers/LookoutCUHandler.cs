using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace DaemonAtorService;

public class LookoutCUHandler : IEmailHandler
{
    private readonly EmailReadJob _job;

    public LookoutCUHandler(EmailReadJob job)
    {
        _job = job;
    }

    public async Task<string> HandleAsync(string subject, Message message, string emailDate, GmailService service)
    {
        var logMessage = await _job.ParseAccountBalanceAlert(subject, message, emailDate);
        if (!string.IsNullOrEmpty(logMessage))
        {
            _job.LogForDashboard("Checking Account " + logMessage, subject, _job._dashboardLogLocation, _job._attachmentSaveLocation, "CUChecking", message, service);
        }
        return logMessage;
    }
}