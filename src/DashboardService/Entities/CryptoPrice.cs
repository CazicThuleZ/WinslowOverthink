using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;
[Table("CryptoPrices")]

public class CryptoPrice
{
    public Guid Id { get; set; } 
    public DateTime SnapshotDateUTC { get; set; } 
    public string CryptoId { get; set; }
    public decimal Price { get; set; }
}
