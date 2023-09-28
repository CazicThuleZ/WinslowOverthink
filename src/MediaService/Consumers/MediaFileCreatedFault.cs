using Contracts;
using MassTransit;

namespace MediaService.Consumers
{
    public class MediaFileCreatedFaultConsumer : IConsumer<Fault<MediaFileCreated>>
    {
        public async Task Consume(ConsumeContext<Fault<MediaFileCreated>> context)
        {
            Console.WriteLine($"Consuming faulty creation: {context.Message.Exceptions}");

            var exception = context.Message.Exceptions.First();
            if(exception.ExceptionType == "System.ArgumentException")
            {
                context.Message.Message.Description = "Not provided";
                await context.Publish(context.Message.Message);
            }
            else
            {
                Console.WriteLine("Not an arguement exception.  This is not handled currently and refactorin is necessary.");
            }
        }
    }
};

