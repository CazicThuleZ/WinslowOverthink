using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
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
        private readonly IPublishEndpoint _publishEndpoint;

        public MediaFilesController(MediaDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
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

            var publishedVideoFile = _mapper.Map<VideoFileDto>(newVideoFile);
            await _publishEndpoint.Publish(_mapper.Map<MediaFileCreated>(publishedVideoFile));            

            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Unable to create new VideoFile");

            return CreatedAtAction(nameof(GetVideoFileById), new { id = newVideoFile.Id }, publishedVideoFile);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVideoFile(Guid id, UpdateVideoFileDto updateVideoFileDto)
        {
            var VideoFileToUpdate = await _context.VideoFiles.FirstOrDefaultAsync(x => x.Id == id);
            if (VideoFileToUpdate == null)
                return NotFound();

            VideoFileToUpdate.DiskVolumeName = updateVideoFileDto.DiskVolumeName ?? VideoFileToUpdate.DiskVolumeName;
            VideoFileToUpdate.FilePath = updateVideoFileDto.FilePath ?? VideoFileToUpdate.FilePath;

            await _publishEndpoint.Publish(_mapper.Map<MediaFileUpdated>(VideoFileToUpdate));

            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Unable to update VideoFile");

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVideoFile(Guid id)
        {
            var VideoFileToDelete = await _context.VideoFiles.FindAsync(id);
            if (VideoFileToDelete == null)
                return NotFound();

            _context.VideoFiles.Remove(VideoFileToDelete);

            await _publishEndpoint.Publish<MediaFileDeleted>(new { Id = VideoFileToDelete.Id.ToString() });

            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Unable to delete VideoFile");

            return NoContent();
        }
    }
}