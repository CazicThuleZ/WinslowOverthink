using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaService.Data;
using MediaService.Entities;

namespace VideoFileService.IntegrationTests.Util
{
    public static class DbHelper
    {
        public static void InitDbForTests(MediaDbContext db)
        {
            db.VideoFiles.AddRange(GetVideoFilesForTest());
            db.SaveChanges();
        }

        public static void ReinitDbForTests(MediaDbContext db)
        {
            db.VideoFiles.RemoveRange(db.VideoFiles);
            db.SaveChanges();
            InitDbForTests(db);
        }
        private static List<VideoFile> GetVideoFilesForTest()
        {
            return new List<VideoFile>
            {
                new VideoFile
                {
                    Id = Guid.Parse("3c3d07d3-03c9-497a-a5a9-decf50219288"),
                    FileName = "test1.mp4",
                    FilePath = "/path/to/test1.mp4",
                    Size = 1024,
                    Duration = 60,
                    FileCreateDateUTC = DateTime.UtcNow
                },
                new VideoFile
                {
                    Id = Guid.Parse("9a212318-3de1-4b0d-b898-3b286f329b3b"),
                    FileName = "test2.mp4",
                    FilePath = "/path/to/test2.mp4",
                    Size = 2048,
                    Duration = 120,
                    FileCreateDateUTC = DateTime.UtcNow
                },
                new VideoFile
                {
                    Id = Guid.Parse("d8a53e84-0b77-4cb8-aff3-3ac358d52fd7"),
                    FileName = "test3.mp4",
                    FilePath = "/path/to/test3.mp4",
                    Size = 4096,
                    Duration = 180,
                    FileCreateDateUTC = DateTime.UtcNow
                }
            };
        }
    }
}       
