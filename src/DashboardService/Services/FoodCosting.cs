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
        const int batchSize = 100;

        try
        {
            // Process in batches
            var mealLogsWithZeroCost = await _context.MealLogs
                .Where(meal => meal.Cost == 0)
                .Select(m => m.Id)
                .ToListAsync();

            foreach (var batch in mealLogsWithZeroCost.Chunk(batchSize))
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var meals = await _context.MealLogs
                        .Where(m => batch.Contains(m.Id))
                        .ToListAsync();

                    foreach (var mealLog in meals)
                    {
                        decimal calculatedCost = await CalculateFoodCostAsync(
                            mealLog.Name,
                            mealLog.Quantity,
                            mealLog.UnitOfMeasure);

                        if (calculatedCost > 0)
                        {
                            mealLog.Cost = Math.Round(calculatedCost, 2);
                            updatedCount++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating meal log costs", ex);
        }

        return updatedCount;
    }
    public async Task<int> UpdateDietStatCostsAsync()
    {
        var updatedCount = 0;
        const int batchSize = 100;

        try
        {
            var unknownCostDayIds = await _context.DietStats
                .Where(diet => diet.Cost == 0)
                .Select(d => d.Id)
                .ToListAsync();

            foreach (var batch in unknownCostDayIds.Chunk(batchSize))
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var dietStats = await _context.DietStats
                        .Where(d => batch.Contains(d.Id))
                        .ToListAsync();

                    foreach (var day in dietStats)
                    {
                        var mealLogsForDay = await _context.MealLogs
                            .Where(meal =>
                                meal.SnapshotDateUTC.Date == day.SnapshotDateUTC.Date &&
                                meal.Cost >= 0)
                            .Select(m => new { m.Cost })
                            .ToListAsync();

                        if (!mealLogsForDay.Any())
                            continue;

                        if (mealLogsForDay.All(meal => meal.Cost > 0))  // If we still don't know the cost of a meal item, we don't know the cost for the day.
                        {
                            decimal totalCost = mealLogsForDay.Sum(meal => meal.Cost);
                            day.Cost = Math.Round(totalCost, 2);
                            updatedCount++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
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
