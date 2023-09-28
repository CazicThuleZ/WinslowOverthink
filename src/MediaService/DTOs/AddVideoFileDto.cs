using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MediaService.DTOs
{
    public class AddVideoFileDto
    {
        [Required]
        public string DiskVolumeName { get; set; }
        [Required]
        public string FilePath { get; set; }
        [Required]
        public string FileName { get; set; }
        [Required]
        public string ShowTitle { get; set; }
        [Required]
        public string Description { get; set; }
        public string YearReleased { get; set; }
        public decimal Duration { get; set; }
        public int Size { get; set; }
        public string ThumbnailUrl { get; set; }        
        public string EpisodeTitle { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }        
        public DateTime FileCreateDateUTC { get; set; }
    }
}