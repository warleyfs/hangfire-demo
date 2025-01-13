using HangfireDemo.Contracts.DTOs.RabbitMQ;
using MassTransit;

namespace HangfireDemo.Api.Workers.RabbitMQ;

public class MessageProducer(IBus bus) : IProducer
{
    public async Task PostAsync(Message message, CancellationToken stoppingToken)
    {
        await bus.Publish(message, stoppingToken);
        await Task.Delay(1000, stoppingToken);
    }
}