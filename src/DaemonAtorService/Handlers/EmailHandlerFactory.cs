namespace DaemonAtorService;

public class EmailHandlerFactory
{
    private readonly EmailReadJob _job;

    public EmailHandlerFactory(EmailReadJob job)
    {
        _job = job;
    }
    public IEmailHandler GetHandler(string subject)
    {
        if (subject.Contains("Lookout CU balance alert", StringComparison.OrdinalIgnoreCase))
            return new LookoutCUHandler(_job);

        if (subject.Contains("Lose It! Daily Summary", StringComparison.OrdinalIgnoreCase))
            return new LoseItHandler(_job);

        if (subject.Contains("Fidelity Alerts", StringComparison.OrdinalIgnoreCase))
            return new FidelityHandler(_job);

        if (subject.Contains("Current Balance Alert", StringComparison.OrdinalIgnoreCase))
            return new DiscoverHandler(_job);

        return null;
    }
}
