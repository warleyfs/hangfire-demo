using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HangfireDemo.Worker.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace HangfireDemo.Worker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        
        await CreateHostBuilder(args).Build().RunAsync();
        Console.WriteLine("Application started.");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var mongoUrlBuilder = new MongoUrlBuilder($"mongodb://{(hostContext.HostingEnvironment.IsDevelopment() ? "localhost" : "mongo")}:27017/jobs");
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
                
                services.AddHangfire(configuration =>
                {
                    configuration.UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, new MongoStorageOptions
                    {
                        CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.Poll,
                        MigrationOptions = new MongoMigrationOptions
                        {
                            MigrationStrategy = new MigrateMongoMigrationStrategy(),
                            BackupStrategy = new CollectionMongoBackupStrategy()
                        },
                        Prefix = "hangfire.mongo",
                        CheckConnection = false
                    });
                });

                services.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
                
                services.AddMassTransit(busConfig =>
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
            });
}