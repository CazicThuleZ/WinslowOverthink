using MongoDB.Entities;

namespace SearchService.Models;

public class Item : Entity
{
        public string DiskVolumeName { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ShowTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;        
        public string YearReleased  { get; set; } = string.Empty;
        public decimal Duration { get; set; } = 0;
        public long Size { get; set; } = 0;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string EpisodeTitle { get; set; } = string.Empty;
        public int SeasonNumber { get; set; } = 0;
        public int EpisodeNumber { get; set; } = 0; 
        public DateTime FileCreateDateUTC { get; set; } = DateTime.UtcNow;
}
