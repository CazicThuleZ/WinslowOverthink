namespace DaemonAtorService;

public class CryptoHistory
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public List<DailyCryptoStat> DailyStats { get; set; } = new List<DailyCryptoStat>();

}

public class DailyCryptoStat
{
    public DateTime Date { get; set; }
    public decimal HighestPrice { get; set; }
}
