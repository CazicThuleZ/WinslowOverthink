using System.ComponentModel.DataAnnotations;

namespace DashboardService.DTOs;

public class VoiceLogDto
{
    [Required]
    public DateTime SnapshotDateUTC { get; set; }
    [Required]    
    public string ActivityName { get; set; }
    [Required]    
    public decimal Quantity { get; set; }
    [Required]    
    public string UnitOfMeasure { get; set; }
}
