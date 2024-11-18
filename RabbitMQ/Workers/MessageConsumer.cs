using MassTransit;
using Newtonsoft.Json;

namespace HangfireDemo.Api.RabbitMQ.Workers;

public class MessageConsumer(ILogger<Message> logger) : IConsumer<Message>
{
    public Task Consume(ConsumeContext<Message> context)
    {
        logger.LogInformation($"Received Message: {JsonConvert.SerializeObject(context.Message)}");
        return Task.CompletedTask;
    }
}