using Google.Apis.Gmail.v1.Data;
using Google.Apis.Gmail.v1;
using MimeKit;

namespace DaemonAtorService;

public interface IEmailHandler
{
    public Task HandleAsync(string subject, MimeMessage message, string emailDate, ILoggingStrategy loggingStrategy);
}
