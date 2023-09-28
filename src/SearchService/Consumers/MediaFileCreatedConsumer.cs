using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService;

public class MediaFileCreatedConsumer : IConsumer<MediaFileCreated>
{
    private readonly IMapper _mapper;

    public MediaFileCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    public async Task Consume(ConsumeContext<MediaFileCreated> context)
    {
        Console.WriteLine($"Consuming MediaFileCreated event: {context.Message.Id}");        
        
        var item = _mapper.Map<Item>(context.Message);

        if(item.Description == "foo") throw new ArgumentException("foo is not permitted value for description");

        await item.SaveAsync();
    }
}
