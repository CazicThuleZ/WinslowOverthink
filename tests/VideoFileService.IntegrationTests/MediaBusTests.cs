
using System.Net;
using System.Net.Http.Json;
using Contracts;
using MassTransit.Testing;
using MediaService.Data;
using MediaService.DTOs;
using Microsoft.Extensions.DependencyInjection;
using VideoFileService.IntegrationTests.Util;

namespace VideoFileService.IntegrationTests;

[Collection("Shared collection")]
public class MediaBusTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;
    private ITestHarness _testHarness;

    public MediaBusTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
        _testHarness = _factory.Services.GetTestHarness();
    }

    [Fact]
    public async Task CreateAuction_WithValidObject_ShouldPublishAuctionCreated()
    {
        // arrange
        var auction = GetMediaFileForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync("api/MediaFiles", auction);

        // assert
        response.EnsureSuccessStatusCode();
        Assert.True(await _testHarness.Published.Any<MediaFileCreated>());
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
