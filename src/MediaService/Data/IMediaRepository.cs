using MediaService.DTOs;
using MediaService.Entities;

namespace MediaService;

public interface IMediaRepository
{
    Task<List<VideoFileDto>> GetAllVideoFiles(string date);
    Task<VideoFileDto> GetVideoFileByIdAsync(Guid id);
    Task<VideoFile> GetVideoFileEntityByIdAsync(Guid id);
    void AddVideoFile(VideoFile videoFile);
    void RemoveVideoFile(VideoFile videoFile);
    Task<bool> SaveChangesAsync();

}
