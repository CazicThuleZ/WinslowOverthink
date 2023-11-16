using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediaService.Data;
using MediaService.DTOs;
using MediaService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService;

public class MediaRepository : IMediaRepository
{
    private readonly MediaDbContext _context;
    private readonly IMapper _mapper;

    public MediaRepository(MediaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    public void AddVideoFile(VideoFile videoFile)
    {
        _context.VideoFiles.Add(videoFile);
    }

    public async Task<List<VideoFileDto>> GetAllVideoFiles(string date)
    {
            var query = _context.VideoFiles.OrderBy(x => x.FileCreateDateUTC).AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.FileCreateDateUTC.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            return await query.ProjectTo<VideoFileDto>(_mapper.ConfigurationProvider).ToListAsync();
    }

    public async Task<VideoFileDto> GetVideoFileByIdAsync(Guid id)
    {
        return await _context.VideoFiles
            .ProjectTo<VideoFileDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(x => x.Id == id);        
    }

    public async Task<VideoFile> GetVideoFileEntityByIdAsync(Guid id)
    {
            return await _context.VideoFiles.FirstOrDefaultAsync(x => x.Id == id);
    }

    public void RemoveVideoFile(VideoFile videoFile)
    {
        _context.VideoFiles.Remove(videoFile);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
