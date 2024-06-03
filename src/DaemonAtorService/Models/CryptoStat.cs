using System.Text.Json.Serialization;

namespace DaemonAtorService;

public class CryptoStat
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public decimal Current_Price { get; set; }
    [JsonConverter(typeof(JsonStringLongConverter))]
    public long Market_Cap { get; set; }
    public int Market_Cap_Rank { get; set; }
    [JsonConverter(typeof(JsonStringLongConverter))]
    public long? Fully_Diluted_Valuation { get; set; }
    [JsonConverter(typeof(JsonStringLongConverter))]
    public long Total_Volume { get; set; }
    public decimal High_24h { get; set; }
    public decimal Low_24h { get; set; }
    public decimal Price_Change_24h { get; set; }
    public decimal Price_Change_Percentage_24h { get; set; }
    [JsonConverter(typeof(JsonStringLongConverter))]
    public long Market_Cap_Change_24h { get; set; }
    public decimal Market_CapChange_Percentage_24h { get; set; }
    public decimal Circulating_Supply { get; set; }
    public decimal? Total_Supply { get; set; }
    public decimal? Max_Supply { get; set; }
    public decimal Ath { get; set; }
    public decimal Ath_Change_Percentage { get; set; }
    public DateTime Ath_Date { get; set; }
    public decimal Atl { get; set; }
    public decimal Atl_Change_Percentage { get; set; }
    public DateTime Atl_Date { get; set; }
    public Roi Roi { get; set; }
    public DateTime Last_Updated { get; set; }
}

public class Roi
{
    public decimal Times { get; set; }
    public string Currency { get; set; }
    public decimal Percentage { get; set; }
}
