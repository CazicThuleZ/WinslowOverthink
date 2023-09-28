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
    }
}