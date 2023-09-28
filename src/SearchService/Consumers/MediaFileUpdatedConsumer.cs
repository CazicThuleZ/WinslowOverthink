using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService;

public class MediaFileUpdatedConsumer : IConsumer<MediaFileUpdated>
{
    private readonly IMapper _mapper;
    public MediaFileUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;            
    }
    public async Task Consume(ConsumeContext<MediaFileUpdated> context)
    {
        Console.WriteLine($"Consuming MediaFileUpdated event: {context.Message.Id}");        
        
        var item = _mapper.Map<Item>(context.Message);

        var result = await DB.Update<Item>()
            .Match(x => x.ID == context.Message.Id)
            .ModifyOnly(x => new            
            {
                x.DiskVolumeName,
                x.FilePath
             },item)
            .ExecuteAsync();

        if (!result.IsAcknowledged)
            Console.WriteLine($"Problem updating MongoDB: {context.Message.Id}");
    }
}
