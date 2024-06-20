using DashboardService.Entities;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Data;

public class DashboardDbContext : DbContext
{
    public DashboardDbContext(DbContextOptions options) : base(options)
    {
    }
    public DbSet<DietStat> DietStats { get; set; } = null!;

    public DbSet<AccountBalance> AccountBalances { get; set; } = null!;

    public DbSet<CryptoPrice> CryptoPrices { get; set; } = null!;
}
