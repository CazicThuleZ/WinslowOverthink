namespace DaemonAtorService;

public class LogAccountBalance
{
    public DateTime SnapshotDateUTC { get; set; }
    public string AccountName { get; set; }
    public decimal Balance { get; set; }

}
