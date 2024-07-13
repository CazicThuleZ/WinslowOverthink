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
      public DbSet<FoodPrice> FoodPrices { get; set; } = null!;
      public DbSet<ActivityDuration> ActivityDuration { get; set; } = null!;
      public DbSet<ActivityCount> ActivityCount { get; set; } = null!;
      public DbSet<VoiceLog> VoiceLogs { get; set; } = null!;
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<FoodPrice>(entity =>
            {
                  entity.HasKey(e => e.Id);

                  // Create composite index
                  entity.HasIndex(e => new { e.Name, e.UnitOfMeasure })
                    .IsUnique();

                  // Other property configurations
                  entity.Property(e => e.LastUpdateDateUTC)
                    .HasColumnType("timestamp with time zone");

                  entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                  entity.Property(e => e.UnitOfMeasure)
                    .IsRequired()
                    .HasColumnType("text");

                  entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");
            });
      }
}
