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
    public async Task HandleAsync(string subject, Message message, string emailDate, GmailService service, ILoggingStrategy loggingStrategy)
    {
        var accountBalance = await _job.ParseAccountBalanceAlert(subject, message, emailDate);
        accountBalance.AccountName = "Discover Savings Account";
        loggingStrategy.Log(accountBalance);
    }
}
