namespace HangfireDemo.Api.RabbitMQ;

public interface IProducer
{
    Task PostAsync(Message message, CancellationToken stoppingToken);
}