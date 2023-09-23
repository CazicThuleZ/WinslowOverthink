using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediaService.Data;
using MediaService.DTOs;
using MediaService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaFilesController : ControllerBase
    {
        private readonly MediaDbContext _context;
        private readonly IMapper _mapper;

        public MediaFilesController(MediaDbContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;            
        }

        [HttpGet]
        public async Task<ActionResult<List<VideoFileDto>>> GetAllVideoFiles(string date)
        {
            var query = _context.VideoFiles.OrderBy(x => x.FileCreateDateUTC).AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.FileCreateDateUTC.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            return await query.ProjectTo<VideoFileDto>(_mapper.ConfigurationProvider).ToListAsync();

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VideoFileDto>> GetVideoFileById(Guid id)
        {
            var VideoFile = await _context.VideoFiles.FirstOrDefaultAsync(x => x.Id == id); 
            if (VideoFile == null)
                return NotFound();
            
            return _mapper.Map<VideoFileDto>(VideoFile);
        }

        [HttpPost]
        public async Task<ActionResult<AddVideoFileDto>> CreateVideoFile(AddVideoFileDto addVideoFileDto)
        {
            var newVideoFile = _mapper.Map<VideoFile>(addVideoFileDto);
            newVideoFile.Id = Guid.NewGuid();
            _context.VideoFiles.Add(newVideoFile);

            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Unable to create new VideoFile");

            return CreatedAtAction(nameof(GetVideoFileById), new { id = newVideoFile.Id }, _mapper.Map<VideoFileDto>(newVideoFile));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVideoFile(Guid id, UpdateVideoFileDto updateVideoFileDto)
        {
            var VideoFileToUpdate = await _context.VideoFiles.FirstOrDefaultAsync(x => x.Id == id);
            if (VideoFileToUpdate == null)
                return NotFound();

            VideoFileToUpdate.Duration = updateVideoFileDto.Duration ?? VideoFileToUpdate.Duration;
            VideoFileToUpdate.Description = updateVideoFileDto.Description ?? VideoFileToUpdate.Description;
            VideoFileToUpdate.EpisodeNumber = updateVideoFileDto.EpisodeNumber ?? VideoFileToUpdate.EpisodeNumber;
            VideoFileToUpdate.EpisodeTitle = updateVideoFileDto.EpisodeTitle ?? VideoFileToUpdate.EpisodeTitle;
            VideoFileToUpdate.FileCreateDateUTC = updateVideoFileDto.FileCreateDateUTC ?? VideoFileToUpdate.FileCreateDateUTC;
            VideoFileToUpdate.FilePath = updateVideoFileDto.FilePath ?? VideoFileToUpdate.FilePath;
            VideoFileToUpdate.FileName = updateVideoFileDto.FileName ?? VideoFileToUpdate.FileName;
            VideoFileToUpdate.SeasonNumber = updateVideoFileDto.SeasonNumber ?? VideoFileToUpdate.SeasonNumber;
            VideoFileToUpdate.ShowTitle = updateVideoFileDto.ShowTitle ?? VideoFileToUpdate.ShowTitle;
            VideoFileToUpdate.Size = updateVideoFileDto.Size ?? VideoFileToUpdate.Size;
            VideoFileToUpdate.ThumbnailUrl = updateVideoFileDto.ThumbnailUrl ?? VideoFileToUpdate.ThumbnailUrl;

            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Unable to update VideoFile");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVideoFile(Guid id)
        {
            var VideoFileToDelete = await _context.VideoFiles.FindAsync(id);
            if (VideoFileToDelete == null)
                return NotFound();

            _context.VideoFiles.Remove(VideoFileToDelete);

            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Unable to delete VideoFile");

            return NoContent();
        }
    }
}