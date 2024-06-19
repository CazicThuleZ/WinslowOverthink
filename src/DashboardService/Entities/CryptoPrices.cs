using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;
[Table("CryptoPrices")]

public class CryptoPrices
{
    public DateTime SnapshotDateUTC { get; set; }
    public string CryptoId { get; set; }
    public decimal Price { get; set; }

}
