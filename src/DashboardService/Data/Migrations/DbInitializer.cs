using DashboardService.Data;
using DashboardService.Entities;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Data;

public class DbInitializer
{
    public static void InitDb(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        SeedData(scope.ServiceProvider.GetService<DashboardDbContext>());

    }

    private static void SeedData(DashboardDbContext context)
    {
        context.Database.Migrate();

        if (!context.DietStats.Any())
        {
            var dsOptions = new List<DietStat>()
                {
                    new DietStat()
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUTC = DateTime.UtcNow,
                        Weight = 200,
                        Calories = 2000,
                        ProteinGrams = 100.5m,
                        FatGrams = 50.5m,
                        CarbGrams = 150.5m,
                        FiberGrams = 25.5m,
                        SugarGrams = 25.5m,
                        SaturatedFatGrams = 25.5m,
                        SodiumMilliGrams = 25.5m,
                        ExerciseCalories = -500
                    },
                    new DietStat()
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUTC = DateTime.UtcNow,
                        Weight = 195,
                        Calories = 1900,
                        ProteinGrams = 85.5m,
                        FatGrams = 40.5m,
                        CarbGrams = 120.5m,
                        FiberGrams = 15.5m,
                        SugarGrams = 66.5m,
                        SaturatedFatGrams = 20.5m,
                        SodiumMilliGrams = 12.3m,
                        ExerciseCalories = -445
                    }
                };

            context.DietStats.AddRange(dsOptions);
            context.SaveChanges();
        }

        if (!context.AccountBalances.Any())
        {
            var abOptions = new List<AccountBalance>()
                {
                    new AccountBalance()
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUTC = DateTime.UtcNow,
                        AccountName = "Checking",
                        Balance = 1000.00m
                    },
                    new AccountBalance()
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUTC = DateTime.UtcNow,
                        AccountName = "Discover",
                        Balance = 10000.00m
                    }
                };

            context.AccountBalances.AddRange(abOptions);
            context.SaveChanges();
        }

        if (!context.CryptoPrices.Any())
        {
            var cpOptions = new List<CryptoPrice>()
                {
                    new CryptoPrice()
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUTC = DateTime.UtcNow,
                        CryptoId = "Bitcoin",
                        Price = 47000.00m
                    },
                    new CryptoPrice()
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUTC = DateTime.UtcNow,
                        CryptoId = "Ethereum",
                        Price = 3600.12m
                    }
                };
            context.CryptoPrices.AddRange(cpOptions);
            context.SaveChanges();
        }
    }
}
