using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;
[Table("AccountBalances")]

public class AccountBalances
{
    public DateTime SnapshotDateUTC { get; set; }
    public string AccountName { get; set; }
    public decimal Balance { get; set; }
}
