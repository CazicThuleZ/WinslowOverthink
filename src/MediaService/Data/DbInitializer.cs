using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Data
{
    public class DbInitializer
    {
        public static void InitDb(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            SeedData(scope.ServiceProvider.GetService<MediaDbContext>());

        }

        private static void SeedData(MediaDbContext context)
        {
            context.Database.Migrate();

            if(!context.DiskVolumes.Any())
            {
                var dvOptions = new List<DiskVolume>()
                {
                    new DiskVolume()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Dummy",
                        AvailableSpace = 1000
                    }
                };

                context.DiskVolumes.AddRange(dvOptions);
                context.SaveChanges();
            }

            if(!context.VideoFiles.Any())
            {
                var vfOptions = new List<VideoFile>()
                {
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Dummy",
                        Description = "Dummy",
                        Size = 1000,
                        FileName = "Dummy.txt",
                        FilePath = @"c:\Dummy.txt",                        
                        DiskVolumeId = context.DiskVolumes.FirstOrDefault().Id,
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "Dummy",
                    }
                };

                context.VideoFiles.AddRange(vfOptions);
                context.SaveChanges();
            }
 
        }
    }
}