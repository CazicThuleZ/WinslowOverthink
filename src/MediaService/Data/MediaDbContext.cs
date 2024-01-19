using MassTransit;
using MediaService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Data;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<VideoFile> VideoFiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<VideoFile>()
            .HasIndex(v => new { v.ShowTitle, v.SeasonNumber, v.EpisodeNumber })
            .HasDatabaseName("Index_ShowTitle_SeasonNumber_EpisodeNumber");

    }

}
