using System.ComponentModel.DataAnnotations;

namespace DashboardService.DTOs;

public class AccountBalanceDto
{
    [Required]
    public DateTime SnapshotDateUTC { get; set; }
    [Required]
    public string AccountName { get; set; }
    [Required]
    public decimal Balance { get; set; }
}
