using Hangfire;
using HangfireDemo.Api.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireDemo.Worker.RabbitMQ;

public class MessageConsumer(ILogger<Message> logger) : IConsumer<Message>
{
    public Task Consume(ConsumeContext<Message> context)
    {
        logger.LogInformation($"{Environment.MachineName} Received Message: {JsonConvert.SerializeObject(context.Message)}");
        
        // Simula uma situação de erro.
        if (context.GetRetryCount() == 0 && context.Message.ForceRetry)
        {
            const string logMessage = "Throw Exception to force message retry.";
            logger.LogError(logMessage);
            throw new ApplicationException(logMessage);
        }

        new BackgroundJobClient().Schedule(context.Message.Queue, 
            () => Console.WriteLine(context.Message.Content), 
            context.Message.Delay
        );

        return Task.CompletedTask;
    }
}