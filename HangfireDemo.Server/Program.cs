using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HangfireDemo.Contracts.DTOs.Hangfire;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace HangfireDemo.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var mongoUrlBuilder = new MongoUrlBuilder($"mongodb://{(builder.Environment.IsDevelopment() ? "localhost" : "mongo")}:27017/jobs");
        var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

        // Add Hangfire services. Hangfire.AspNetCore nuget required
        builder.Services.AddHangfire(configuration => 
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
        builder.Services.AddHangfireServer(serverOptions =>
        {
            serverOptions.SchedulePollingInterval = TimeSpan.FromMinutes(1);
            serverOptions.ServerName = $"{Environment.MachineName}-{Random.Shared.Next()}";
            serverOptions.Queues = JobEngineConfig.Queues;
        });

        Console.ReadKey();
    }
}