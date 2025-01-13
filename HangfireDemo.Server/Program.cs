using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HangfireDemo.Contracts.DTOs.Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace HangfireDemo.Server;

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

                // Add Hangfire services. Hangfire.AspNetCore nuget required
                services.AddHangfire(configuration => 
                    configuration
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, new MongoStorageOptions
                        {
                            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.Poll,
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            },
                            Prefix = "hangfire.mongo",
                            CheckConnection = false
                        })
                );

                // Add the processing server as IHostedService
                services.AddHangfireServer(serverOptions =>
                {
                    serverOptions.SchedulePollingInterval = TimeSpan.FromMinutes(1);
                    serverOptions.ServerName = $"{Environment.MachineName}-{Random.Shared.Next()}";
                    serverOptions.Queues = JobEngineConfig.Queues;
                });
            });
}