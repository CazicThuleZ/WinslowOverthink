using System.ComponentModel.DataAnnotations;

namespace DashboardService.DTOs;

public class CryptoPriceDto
{
    [Required]
    public DateTime SnapshotDateUTC { get; set; }
    [Required]
    public string CryptoId { get; set; }
    [Required]
    public decimal Price { get; set; }
}
