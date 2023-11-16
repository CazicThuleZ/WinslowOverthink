
using System.Net;
using System.Net.Http.Json;
using MassTransit.Futures.Contracts;
using MediaService.Data;
using MediaService.DTOs;
using MediaService.Entities;
using Microsoft.Extensions.DependencyInjection;
using VideoFileService.IntegrationTests.Util;

namespace VideoFileService.IntegrationTests;

[Collection("Shared collection")]
public class MediaFileControllerTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;

    private const string TV_ID = "d8a53e84-0b77-4cb8-aff3-3ac358d52fd7";

    public MediaFileControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();

    }

    [Fact]
    public async Task GetMediaFiles_ShouldReturn3Files()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetFromJsonAsync<List<VideoFileDto>>("/api/mediafiles");

        // Assert
        Assert.Equal(3, response.Count);

    }

    [Fact]
    public async Task GetMediaFilesById_WithValidIdShouldReturnVideoFile()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetFromJsonAsync<VideoFileDto>($"/api/mediafiles/{TV_ID}");

        // Assert
        Assert.Equal("test3.mp4", response.FileName);

    }

    [Fact]
    public async Task GetMediaFilesById_WithInValidIdShouldReturn404()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync($"/api/mediafiles/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    }

    [Fact]
    public async Task GetMediaFilesById_WithInValidGuidReturn400()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync("/api/mediafiles/notaguid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    }

    [Fact]
    public async Task CreateMediaFiles_WithNoAuth_ShouldReturn401()
    {
        // Arrange
        var VideoFile = new AddVideoFileDto() { DiskVolumeName = "x" };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/mediafiles", VideoFile);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

    }

    [Fact]
    public async Task CreateMediaFiles_WithAuth_ShouldReturn201()
    {
        // Arrange
        var VideoFile = GetMediaFileForCreate();        
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/mediafiles", VideoFile);

        // Assert
         response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    }

    [Fact]
    public async Task CreateMediaFiles_WithInvalidDto_ShouldReturn400()
    {
        // Arrange
        var VideoFile = GetMediaFileForCreate();        
        VideoFile.DiskVolumeName = null;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/mediafiles", VideoFile);

        // Assert         
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVideoFile_WithValidUpdateDtoAndInvalidUser_ShouldReturn200()
    {
        // arrange? 
        var updateVideoFile = new UpdateVideoFileDto { FilePath = @"D:\New" };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/MediaFiles/{TV_ID}", updateVideoFile);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }    

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        DbHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }

    private AddVideoFileDto GetMediaFileForCreate()
    {
        return new AddVideoFileDto
        {
            DiskVolumeName = "test",
            FileName = "test.mp4",
            Duration = 100,
            FilePath = "/Library/orchid_avon_dynamic.ggb",
            FileCreateDateUTC = DateTime.UtcNow,
            SeasonNumber = 1,
            EpisodeNumber = 1,
            EpisodeTitle = "test",
            ShowTitle = "test",
            Description = "Omnis totam sit dignissimos dolore id inventore nulla veritatis. Vel dolores voluptatibus natus nisi doloremque. Ratione magni saepe vero sit fuga odit et nostrum explicabo. Consequatur eligendi non ut sint quia et ipsam dolor.",
        };
    }
}
