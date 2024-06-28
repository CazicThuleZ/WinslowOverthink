using System.ComponentModel.DataAnnotations.Schema;
namespace DashboardService.Entities;
[Table("FoodPrices")]
public class FoodPrice
{
    // Quantity = 1 always assumed
    public Guid Id { get; set; }
    public DateTime LastUpdateDateUTC { get; set; }
    public string Name { get; set; }
    public string UnitOfMeasure { get; set; }
    public decimal Price { get; set; } 

}
