using Google.Apis.Gmail.v1.Data;
using Google.Apis.Gmail.v1;

namespace DaemonAtorService;

public interface IEmailHandler
{
    Task<string> HandleAsync(string subject, Message message, string emailDate, GmailService service);
}
