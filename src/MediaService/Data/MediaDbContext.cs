using MediaService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Data;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<VideoFile> VideoFiles { get; set; } = null!;
    public DbSet<DiskVolume> DiskVolumes { get; set; } = null!;
    
}
