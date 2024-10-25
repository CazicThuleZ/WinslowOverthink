using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MimeKit;

namespace DaemonAtorService;

public class DiscoverHandler : IEmailHandler
{
    private readonly EmailReadJob _job;

    public DiscoverHandler(EmailReadJob job)
    {
        _job = job;        
    }

    public async Task HandleAsync(string subject, MimeMessage message, string emailDate, ILoggingStrategy loggingStrategy)
    {
        var accountBalance = await _job.ParseAccountBalanceAlert(subject, message, emailDate);
        accountBalance.AccountName = "Discover Savings Account";
        loggingStrategy.Log(accountBalance);
    }
}
