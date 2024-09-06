using System;
using DashboardService.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Services;

public class FoodCosting
{
    private readonly DashboardDbContext _context;
    public FoodCosting(DashboardDbContext context)
    {
        _context = context;
    }

    public async Task<int> UpdateMealLogCostsAsync()
    {
        var updatedCount = 0;
        try
        {
            var mealLogsWithZeroCost = await _context.MealLogs
                .Where(meal => meal.Cost == 0)
                .ToListAsync();

            foreach (var mealLog in mealLogsWithZeroCost)
            {
                decimal calculatedCost = await CalculateFoodCostAsync(mealLog.Name, mealLog.Quantity, mealLog.UnitOfMeasure);

                if (calculatedCost > 0)
                {
                    mealLog.Cost = calculatedCost;
                    updatedCount++;
                };
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating meal log costs", ex);
        }

        return updatedCount;

    }
    public async Task<decimal> UpdateDietStatCostsAsync()
    {
        var updatedCount = 0;

        try
        {
            var unknownCostDays = await _context.DietStats
                .Where(diet => diet.Cost == 0)
                .ToListAsync();

            foreach (var day in unknownCostDays)
            {
                var mealLogsForDay = await _context.MealLogs
                    .Where(meal => meal.SnapshotDateUTC.Date == day.SnapshotDateUTC.Date)
                    .ToListAsync();

                if (mealLogsForDay.Count == 0)
                    continue;

                if (mealLogsForDay.All(meal => meal.Cost > 0)) 
                    continue;

                decimal totalCost = mealLogsForDay.Sum(meal => meal.Cost);
                day.Cost = totalCost;
                updatedCount++;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating diet stat costs", ex);
        }

        return updatedCount;
    }
    public async Task<decimal> CalculateFoodCostAsync(string name, decimal quantity, string unitOfMeasure)
    {
        var foodPrice = await _context.FoodPrices
           .FirstOrDefaultAsync(fp =>
            fp.Name.ToLower() == name.ToLower() &&
            fp.UnitOfMeasure.ToLower() == unitOfMeasure.ToLower());

        if (foodPrice == null)
            return 0m;

        var totalPrice = foodPrice.Price * quantity;
        return totalPrice;
    }
}
