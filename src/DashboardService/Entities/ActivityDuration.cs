using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;
[Table("ActivityDuration")]
public class ActivityDuration
{
    public Guid Id { get; set; }
    public DateTime SnapshotDateUTC { get; set; }
    public string Name { get; set; }
    public TimeSpan Duration { get; set; }

}
