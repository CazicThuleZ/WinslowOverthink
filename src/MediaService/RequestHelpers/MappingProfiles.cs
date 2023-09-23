using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediaService.DTOs;
using MediaService.Entities;

namespace MediaService.RequestHelpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<AddVideoFileDto,VideoFile>();
            CreateMap<UpdateVideoFileDto,VideoFile>();
            CreateMap<VideoFile, VideoFileDto>();
            
        }        
    }
}