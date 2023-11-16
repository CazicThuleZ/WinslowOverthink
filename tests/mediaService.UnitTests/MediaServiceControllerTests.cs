using AutoFixture;
using AutoMapper;
using MassTransit;
using MediaService;
using MediaService.Controllers;
using MediaService.DTOs;
using MediaService.Entities;
using MediaService.RequestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace mediaService.UnitTests;

public class MediaServiceControllerTests
{
    private readonly Mock<IMediaRepository> _mediaServiceRepo;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Fixture _fixture;
    private readonly MediaFilesController _controller;
    private readonly IMapper _mapper;

    public MediaServiceControllerTests()
    {
        _fixture = new Fixture();
        _mediaServiceRepo = new Mock<IMediaRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();

        var mockMapper = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(MappingProfiles).Assembly);
        }).CreateMapper().ConfigurationProvider;

        _mapper = new Mapper(mockMapper);
        _controller = new MediaFilesController(_mediaServiceRepo.Object, _mapper, _publishEndpoint.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = Helpers.GetClaimsPrincipal()
                }
            }
        };
    }

    [Fact]
    public async Task GetAllVideoFiles_WithNoParams_Returns10Files()
     {
        // Arrange
        var mediaFiles = _fixture.CreateMany<VideoFileDto>(10).ToList();
        _mediaServiceRepo.Setup(x => x.GetAllVideoFiles(null)).ReturnsAsync(mediaFiles);
        
        // Act
        var result = await _controller.GetAllVideoFiles(null);        

        // Assert
        Assert.Equal(10, result.Value.Count);
        Assert.IsType<ActionResult<List<VideoFileDto>>>(result);
    }

    [Fact]
    public async Task GetVideoFileById_WithValidGuid_ReturnsFile()
     {
        // Arrange
        var mediaFile = _fixture.Create<VideoFileDto>();
        _mediaServiceRepo.Setup(x => x.GetVideoFileByIdAsync(It.IsAny<Guid>())).ReturnsAsync(mediaFile);
        
        // Act
        var result = await _controller.GetVideoFileById(mediaFile.Id);

        // Assert
        Assert.Equal(mediaFile.EpisodeTitle, result.Value.EpisodeTitle);
        Assert.IsType<ActionResult<VideoFileDto>>(result);
    }    

    [Fact]
    public async Task GetVideoFileById_WithInValidGuid_ReturnsNotFound()
     {
        // Arrange      
        _mediaServiceRepo.Setup(x => x.GetVideoFileByIdAsync(It.IsAny<Guid>())).ReturnsAsync(value: null);
        
        // Act
        var result = await _controller.GetVideoFileById(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }        

    [Fact]
    public async Task CreateVideoFile_WithAddVideoFileDto_ReturnsCreatedAtActionResult()
     {
        // Arrange      
        var mediaFile = _fixture.Create<AddVideoFileDto>();
        _mediaServiceRepo.Setup(x => x.AddVideoFile(It.IsAny<VideoFile>()));
        _mediaServiceRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        // Act
        var result = await _controller.CreateVideoFile(mediaFile);
        var createdResult = result.Result as CreatedAtActionResult;

        // Assert
        Assert.NotNull(createdResult);
        Assert.Equal("GetVideoFileById",createdResult.ActionName);
        Assert.IsType<VideoFileDto>(createdResult.Value);
    }        

    [Fact]
    public async Task CreateVideoFile_FailedSave_Returns400BadRequest()
    {
        // arrange
        var videoFileDto = _fixture.Create<AddVideoFileDto>();
        _mediaServiceRepo.Setup(repo => repo.AddVideoFile(It.IsAny<VideoFile>()));
        _mediaServiceRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        // act
        var result = await _controller.CreateVideoFile(videoFileDto);

        // assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateVideoFile_WithUpdateVideoFileDto_ReturnsOkResponse()
    {
        // arrange
        var videoFile = _fixture.Build<VideoFile>().Create();
        var updateDto = _fixture.Create<UpdateVideoFileDto>();
        _mediaServiceRepo.Setup(repo => repo.GetVideoFileEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(videoFile);
        _mediaServiceRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.UpdateVideoFile(videoFile.Id, updateDto);

        // assert
        Assert.IsType<OkResult>(result);  
    }

    [Fact]
    public async Task UpdateVideoFile_WithInvalidGuid_ReturnsNotFound()
    {
        // arrange
        var videoFile = _fixture.Build<VideoFile>().Create();
        var updateDto = _fixture.Create<UpdateVideoFileDto>();
        _mediaServiceRepo.Setup(repo => repo.GetVideoFileEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        // act
        var result = await _controller.UpdateVideoFile(videoFile.Id, updateDto);

        // assert
        Assert.IsType<NotFoundResult>(result);
    }


    [Fact]
    public async Task DeleteVideoFile_WithInvalidGuid_Returns404Response()
    {
        // arrange
        var videoFile = _fixture.Build<VideoFile>().Create();
        _mediaServiceRepo.Setup(repo => repo.GetVideoFileEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        // act
        var result = await _controller.DeleteVideoFile(videoFile.Id);

        // assert
        Assert.IsType<NotFoundResult>(result);         

    }
}
