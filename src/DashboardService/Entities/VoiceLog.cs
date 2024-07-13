using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;

[Table("VoiceLogs")]
public class VoiceLog
{
    public Guid Id { get; set; }
    public DateTime SnapshotDateUTC { get; set; }
    public string ActivityName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; }

}
