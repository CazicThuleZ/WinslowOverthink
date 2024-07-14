using System.ComponentModel.DataAnnotations;

namespace WinslowUI.DTOs;

public class DietStatDto
{
    [Required]
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
