using System.Net;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHealthChecksUI(opt =>
{
    opt.SetEvaluationTimeInSeconds(10); //time in seconds between check    
    opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks    
    opt.SetApiMaxActiveRequests(1); //api requests concurrency    
    opt.AddHealthCheckEndpoint("feedback api", "/healthz"); //map health check api    
}).AddInMemoryStorage();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

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

var queuesCount = 20;
var queues = new string[queuesCount];
for (var i = 0; i < queuesCount; i++) queues[i] = $"queue-{i + 1}";

// Add the processing server as IHostedService
builder.Services.AddHangfireServer(serverOptions =>
{
    serverOptions.SchedulePollingInterval = TimeSpan.FromMinutes(1);
    serverOptions.ServerName = $"{Environment.MachineName}-{Random.Shared.Next()}";
    serverOptions.Queues = queues;
});

var app = builder.Build();

app.Logger.LogInformation("Application started!");

app.UseSwagger();
app.UseSwaggerUI();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new DashboardNoAuthorizationFilter()]
});

app.MapHealthChecks("/healthz", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(delegate (Options options) 
{
    options.UIPath = "/healthcheck-ui";
});

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapPost("/job", (ILogger<Program> logger, int jobCount, TimeSpan delay) =>
{
    logger.LogInformation($"Job {jobCount} started");
    for (var i = 0; i < jobCount; i++)
    {
        var index = new Random().Next(queues.Length);
        new BackgroundJobClient().Schedule(queues[index], () => Console.WriteLine("Background job triggered"), delay);
    }
    return HttpStatusCode.Created;
}).WithName("SubmitJob").WithOpenApi();

app.Run();

public class DashboardNoAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext dashboardContext)
    {
        return true;
    }
}