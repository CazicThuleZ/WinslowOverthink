using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardService.Entities;

[Table("MealLogs")]
public class MealLog
{
    public Guid Id { get; set; }
    public DateTime SnapshotDateUTC { get; set; }
    public string Name { get; set; }
    public string MealType { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; }
    public int Calories { get; set; }
    public decimal FatGrams { get; set; }
    public decimal CarbGrams { get; set; }
    public decimal SugarGrams { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal Cost { get; set; }

}
