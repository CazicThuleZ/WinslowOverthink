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

    public async Task HandleAsync(string subject, Message message, string emailDate, GmailService service, ILoggingStrategy loggingStrategy)
    {
        _job.SaveAttachment(_job._attachmentSaveLocation, message, service);
        var scaleWeight = await _job.ParseLoseItSummary(subject, message, emailDate);
        loggingStrategy.Log(scaleWeight);
    } 
}
