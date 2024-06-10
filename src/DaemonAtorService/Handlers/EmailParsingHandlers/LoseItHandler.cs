using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace DaemonAtorService;

public class LoseItHandler : IEmailHandler
{
    private readonly EmailReadJob _job;

    public LoseItHandler(EmailReadJob job)
    {
        _job = job;
    }

    public Task<string> HandleAsync(string subject, Message message, string emailDate, GmailService service)
    {
        var logMessage = _job.ParseLoseItSummary(subject, message, emailDate);
        if (!string.IsNullOrEmpty(logMessage))
        {
            _job.LogForDashboard(logMessage, subject, _job._dashboardLogLocation, _job._attachmentSaveLocation, "CalorieReport", message, service);
        }
        return Task.FromResult(logMessage);
    }    

}
