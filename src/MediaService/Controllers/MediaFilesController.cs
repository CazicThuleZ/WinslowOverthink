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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaFilesController : ControllerBase
    {
        private readonly IMediaRepository _repo;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public MediaFilesController(IMediaRepository repo, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _repo = repo;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<List<VideoFileDto>>> GetAllVideoFiles(string date)
        { 
            return await _repo.GetAllVideoFiles(date);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VideoFileDto>> GetVideoFileById(Guid id)
        {
            var VideoFile = await _repo.GetVideoFileByIdAsync(id);
            if (VideoFile == null)
                return NotFound();
            
            return VideoFile;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<AddVideoFileDto>> CreateVideoFile(AddVideoFileDto addVideoFileDto)
        {
            //var username = User.Identity.Name;  // Not saved to db, mostly this just serves as a how to reminder in case we need it later

            var newVideoFile = _mapper.Map<VideoFile>(addVideoFileDto);
            newVideoFile.Id = Guid.NewGuid();
            _repo.AddVideoFile(newVideoFile);

            var publishedVideoFile = _mapper.Map<VideoFileDto>(newVideoFile);
            await _publishEndpoint.Publish(_mapper.Map<MediaFileCreated>(publishedVideoFile));            

            var result = await _repo.SaveChangesAsync();

            if (!result)
                return BadRequest("Unable to create new VideoFile");

            return CreatedAtAction(nameof(GetVideoFileById), new { id = newVideoFile.Id }, publishedVideoFile);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVideoFile(Guid id, UpdateVideoFileDto updateVideoFileDto)
        {
            var VideoFileToUpdate = await _repo.GetVideoFileEntityByIdAsync(id);
            if (VideoFileToUpdate == null)
                return NotFound();

            VideoFileToUpdate.DiskVolumeName = updateVideoFileDto.DiskVolumeName ?? VideoFileToUpdate.DiskVolumeName;
            VideoFileToUpdate.FilePath = updateVideoFileDto.FilePath ?? VideoFileToUpdate.FilePath;

            await _publishEndpoint.Publish(_mapper.Map<MediaFileUpdated>(VideoFileToUpdate));

            var result = await _repo.SaveChangesAsync();

            if (!result)
                return BadRequest("Unable to update VideoFile");

            return Ok();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVideoFile(Guid id)
        {
            var VideoFileToDelete = await _repo.GetVideoFileEntityByIdAsync(id);
            if (VideoFileToDelete == null)
                return NotFound();

            _repo.RemoveVideoFile(VideoFileToDelete);

            await _publishEndpoint.Publish<MediaFileDeleted>(new { Id = VideoFileToDelete.Id.ToString() });

            var result = await _repo.SaveChangesAsync();

            if (!result)
                return BadRequest("Unable to delete VideoFile");

            return NoContent();
        }
    }
}