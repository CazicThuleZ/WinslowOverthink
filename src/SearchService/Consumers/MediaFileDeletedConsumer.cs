using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService;

public class MediaFileDeletedConsumer : IConsumer<MediaFileDeleted>
{    
    public async Task Consume(ConsumeContext<MediaFileDeleted> context)
    {
        Console.WriteLine($"Consuming MediaFileDeleted event: {context.Message.Id}");        
        var result = await DB.DeleteAsync<Item>(context.Message.Id);

        if (!result.IsAcknowledged)
            Console.WriteLine($"Problem deleting from MongoDB: {context.Message.Id}");  

    }
}
