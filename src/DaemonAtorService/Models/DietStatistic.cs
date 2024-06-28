namespace DaemonAtorService;

public class DietStatistic
{
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Type { get; set; }
    public decimal Quantity { get; set; }
    public string Units { get; set; }
    public int Calories { get; set; }
    public int Deleted { get; set; }
    public decimal Fat { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbohydrates { get; set; }
    public decimal SaturatedFat { get; set; }
    public decimal Sugars { get; set; }
    public decimal Fiber { get; set; }
    public decimal Cholesterol { get; set; }
    public decimal Sodium { get; set; }
}
