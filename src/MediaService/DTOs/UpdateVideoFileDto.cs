using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MediaService.DTOs
{
    public class UpdateVideoFileDto
    {        
        public string DiskVolumeName { get; set; }     
        public string FilePath { get; set; }     
        public string FileName { get; set; }     
        public string ShowTitle { get; set; }     
        public string Description { get; set; }
        public decimal? Duration { get; set; }
        public int? Size { get; set; }
        public string ThumbnailUrl { get; set; }        
        public string EpisodeTitle { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public DateTime? FileCreateDateUTC { get; set; }
    }
}