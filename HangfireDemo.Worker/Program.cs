using HangfireDemo.Worker.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HangfireDemo.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
        Console.WriteLine("Application started.");
        Console.ReadKey();
        
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        
        builder.Services.AddMassTransit(busConfig =>
        {
            busConfig.AddConsumer<MessageConsumer>();
            busConfig.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h => {
                    h.Username("guest");
                    h.Password("guest");
                });
                cfg.ConfigureEndpoints(context);
                cfg.UseMessageRetry(r => r.Interval(10, TimeSpan.FromSeconds(10)));
            });
        });
        
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) => 
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                
            })
}