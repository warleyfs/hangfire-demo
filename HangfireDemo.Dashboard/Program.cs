using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HangfireDemo.Dashboard.Filters;
using MongoDB.Driver;

namespace HangfireDemo.Dashboard;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var mongoUrlBuilder = new MongoUrlBuilder($"mongodb://{(builder.Environment.IsDevelopment() ? "localhost" : "mongo")}:27017/jobs");
        var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddHangfire(configuration =>
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
        
        var app = builder.Build();
        
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new DashboardNoAuthorizationFilter()]
        });
        
        await app.RunAsync();
    }
}