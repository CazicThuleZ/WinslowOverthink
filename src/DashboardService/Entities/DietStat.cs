using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardService.Entities;

[Table("DietStats")]
public class DietStat
{
    public Guid Id { get; set; }
    public DateTime SnapshotDateUTC { get; set; }
    public int Calories { get; set; }
    public int ExerciseCalories { get; set; }
    public decimal CarbGrams { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal FatGrams { get; set; }
    public decimal SaturatedFatGrams { get; set; }
    public decimal SugarGrams { get; set; }
    public decimal CholesterolGrams { get; set; }
    public decimal FiberGrams { get; set; }
    public decimal SodiumMilliGrams { get; set; }
    public decimal Weight { get; set; }
    public decimal Cost { get; set; }
}