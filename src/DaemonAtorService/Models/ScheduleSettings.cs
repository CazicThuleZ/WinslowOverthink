namespace DaemonAtorService;

public class ScheduleSettings
{
    public string CryptoPriceJob { get; set; }
    public string EmailReadJob { get; set; }
    public string JournalIntakeJob { get; set; }
    public string EndOfDayJob { get; set; }
    public string DatabaseSnapshotsJob { get; set; }
    public string NotificationJob { get; set; }
}
