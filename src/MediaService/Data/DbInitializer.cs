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

            if(!context.VideoFiles.Any())
            {
                var vfOptions = new List<VideoFile>()
                {
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Fantasy Island",
                        Description = "DummyDescription1",
                        YearReleased = "2021",
                        Size = 1000,
                        FileName = "DummyFileName1.txt",
                        FilePath = @"c:\DummyFilePath1.txt",                        
                        DiskVolumeName = "One",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle1",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Gilligan's Island",
                        Description = "DummyDescription2",
                        YearReleased = "2022",
                        Size = 1000,
                        FileName = "DummyFileName2.txt",
                        FilePath = @"c:\DummyFilePath2.txt",                        
                        DiskVolumeName = "One",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle2",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Columbo",
                        Description = "DummyDescription3",
                        YearReleased = "2022",
                        Size = 1000,
                        FileName = "DummyFileName3.txt",
                        FilePath = @"c:\DummyFilePath3.txt",                        
                        DiskVolumeName = "Two",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle3",
                    },                    
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "The Adamms Family",
                        Description = "DummyDescription4",
                        YearReleased = "2022",
                        Size = 1000,
                        FileName = "DummyFileName4.txt",
                        FilePath = @"c:\DummyFilePath4.txt",                        
                        DiskVolumeName = "Two",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle4",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Bonanza",
                        Description = "DummyDescription5",
                        YearReleased = "1933",
                        Size = 1000,
                        FileName = "DummyFileName5.txt",
                        FilePath = @"c:\DummyFilePath5.txt",                        
                        DiskVolumeName = "Two",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle5",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Bewitched",
                        Description = "DummyDescription6",
                        YearReleased = "1966",
                        Size = 1000,
                        FileName = "DummyFileName6.txt",
                        FilePath = @"c:\DummyFilePath6.txt",                        
                        DiskVolumeName = "Three",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle6",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "McMillan and Wife",
                        Description = "DummyDescription7",
                        YearReleased = "1966",
                        Size = 1000,
                        FileName = "DummyFileName6.txt",
                        FilePath = @"c:\DummyFilePath7.txt",                        
                        DiskVolumeName = "Three",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle7",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Rawhide",
                        Description = "DummyDescription8",
                        YearReleased = "1980",
                        Size = 1000,
                        FileName = "DummyFileName8.txt",
                        FilePath = @"c:\DummyFilePath8.txt",                        
                        DiskVolumeName = "Three",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle8",
                    },
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Wonderful World of Disney",
                        Description = "DummyDescription9",
                        YearReleased = "1977",
                        Size = 1000,
                        FileName = "DummyFileName9.txt",
                        FilePath = @"c:\DummyFilePath9.txt",                        
                        DiskVolumeName = "Three",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle9",
                    },                                
                    new VideoFile()
                    {
                        Id = Guid.NewGuid(),
                        ShowTitle = "Star Trek",
                        Description = "DummyDescription10",
                        YearReleased = "1944",
                        Size = 1000,
                        FileName = "DummyFileName10.txt",
                        FilePath = @"c:\DummyFilePath10.txt",                        
                        DiskVolumeName = "Three",
                        Duration = 1000,
                        EpisodeNumber = 1,
                        EpisodeTitle = "DummyEpisodeTitle10",
                    },                                                                                                                                                                                                                                                                            
                };

                context.VideoFiles.AddRange(vfOptions);
                context.SaveChanges();
            }
 
        }
    }
}