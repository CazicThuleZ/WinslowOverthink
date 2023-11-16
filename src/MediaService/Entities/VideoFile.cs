using System.ComponentModel.DataAnnotations.Schema;
namespace MediaService.Entities;
[Table("VideoFiles")]
public class VideoFile
{
        public Guid Id { get; set; } 
        public string DiskVolumeName { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ShowTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string YearReleased { get; set; }
        public decimal Duration { get; set; } = 0;
        public int Size { get; set; } = 0;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string EpisodeTitle { get; set; } = string.Empty;
        public int SeasonNumber { get; set; } = 0;
        public int EpisodeNumber { get; set; } = 0; 
        public DateTime FileCreateDateUTC { get; set; } = DateTime.UtcNow;
        public bool IsMovie() => SeasonNumber == 0 && EpisodeNumber == 0;
}
