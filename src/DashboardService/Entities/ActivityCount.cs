using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;
[Table("ActivityCount")]
public class ActivityCount
{
    public Guid Id { get; set; }
    public DateTime SnapshotDateUTC { get; set; }
    public string Name { get; set; }
    public decimal Count { get; set; }
}
