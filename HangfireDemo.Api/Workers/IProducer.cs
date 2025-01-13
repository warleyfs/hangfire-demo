using HangfireDemo.Contracts.DTOs.RabbitMQ;

namespace HangfireDemo.Api.Workers;

public interface IProducer
{
    Task PostAsync(Message message, CancellationToken stoppingToken);
}