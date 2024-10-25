using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MimeKit;

namespace DaemonAtorService;

public class LoseItHandler : IEmailHandler
{
    private readonly EmailReadJob _job;

    public LoseItHandler(EmailReadJob job)
    {
        _job = job;
    }

    public async Task HandleAsync(string subject, MimeMessage message, string emailDate, ILoggingStrategy loggingStrategy)
    {
        _job.SaveAttachment(_job._attachmentSaveLocation, message);
        var scaleWeight = await _job.ParseLoseItSummary(subject, message, emailDate);
        loggingStrategy.Log(scaleWeight);
    }
}
