using Contracts;
using MassTransit;
using MediaService.Data;

namespace MediaService;

public class MediaFileTranscribedConsumer : IConsumer<MediaFileTranscribed>
{
    private readonly MediaDbContext _dbContext;

    public MediaFileTranscribedConsumer(MediaDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task Consume(ConsumeContext<MediaFileTranscribed> context)
    {
        Console.WriteLine("Consuming MediaFileTranscribed event");
        var mediaFile = await _dbContext.VideoFiles.FindAsync(context.Message.Id);

        // add logic to link the transcription to the video file
    }
}
