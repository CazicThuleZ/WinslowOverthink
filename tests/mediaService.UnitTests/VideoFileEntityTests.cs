using MediaService.Entities;

namespace mediaService.UnitTests;

public class VideoFileEntityTests
{
    [Fact]
    public void IsMovie_SeriesSeasonNumberEqZero_True()
    {
        // Arrange
        var videoFile = new VideoFile
        {
            Id = Guid.NewGuid(),
            SeasonNumber = 0,
            EpisodeNumber = 0
        };

        // Act
        var result = videoFile.IsMovie();

        // Assert
        Assert.True(result);

    }

    [Fact]
    public void IsMovie_SeriesSeasonNumberNeZero_False()
    {
        // Arrange
        var videoFile = new VideoFile
        {
            Id = Guid.NewGuid(),
            SeasonNumber = 1,
            EpisodeNumber = 1
        };

        // Act
        var result = videoFile.IsMovie();

        // Assert
        Assert.False(result);

    }

}