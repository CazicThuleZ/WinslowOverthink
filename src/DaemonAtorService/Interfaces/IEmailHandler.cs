using Google.Apis.Gmail.v1.Data;
using Google.Apis.Gmail.v1;

namespace DaemonAtorService;

public interface IEmailHandler
{
    public Task HandleAsync(string subject, Message message, string emailDate, GmailService service, ILoggingStrategy loggingStrategy);
}
