using System.ComponentModel.DataAnnotations;

namespace DaemonAtorService;

public class MealLogDto
{
    [Required]
    public DateTime SnapshotDateUTC { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string MealType { get; set; }
    [Required]
    public decimal Quantity { get; set; }
    [Required]
    public string UnitOfMeasure { get; set; }
    public int Calories { get; set; }
    public decimal FatGrams { get; set; }
    public decimal CarbGrams { get; set; }
    public decimal SugarGrams { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal Cost { get; set; }
}
