using MassTransit;

namespace HangfireDemo.Api.RabbitMQ.Workers;

public class MessageProducer(IBus bus)
{
    public async Task PostAsync(Message message, CancellationToken stoppingToken)
    {
        await bus.Publish(message, stoppingToken);
        await Task.Delay(1000, stoppingToken);
    }
}