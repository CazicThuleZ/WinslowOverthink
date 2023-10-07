using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService;

public class MediaFileTranscribedConsumer : IConsumer<MediaFileTranscribed>
{
    public async Task Consume(ConsumeContext<MediaFileTranscribed> context)
    {
            Console.WriteLine("Consuming MediaFileTranscribed event");

            var mediaFile = await DB.Find<Item>().OneAsync(context.Message.FileName);

            // todo update something
    }
}
