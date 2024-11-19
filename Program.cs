using System.Net;
using System.Net.Mime;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HangfireDemo.Api.DTOs;
using HangfireDemo.Api.Filters;
using HangfireDemo.Api.RabbitMQ;
using HangfireDemo.Api.RabbitMQ.Workers;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddMassTransit(busConfig =>
{
    busConfig.AddConsumer<MessageConsumer>();
    busConfig.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
        cfg.UseMessageRetry(r => r.Interval(10, TimeSpan.FromSeconds(10)));
    });
});
builder.Services.AddScoped<IProducer, MessageProducer>();

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

var queuesCount = 4;
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
    ResponseWriter = async (context, report) =>
    {
        var response = JsonConvert.SerializeObject(new
        {
            status = report.Status.ToString(),
            currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
            currentTimeZone = TimeZoneInfo.Local.DisplayName,
            server = $"{Environment.MachineName}",
            healthChecks = report.Entries.Select(e => new
            {
                e.Key, 
                e.Value.Status, 
                e.Value.Description, 
                e.Value.Duration,
                e.Value.Exception?.Message,
                e.Value.Tags,
                e.Value.Data
            })
        });
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(response);
    }
});

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapPost("/job", async (ILogger<Program> logger, IProducer producer, [FromBody] SubmitJobRequest request, CancellationToken stoppingToken = new ()) =>
{
    logger.LogInformation($"Job {request.JobCount} started");
    var indexToThrow = request.ForceRetry ? new Random().Next(request.JobCount) : -1;
    
    for (var i = 0; i < request.JobCount; i++)
    {
        var index = new Random().Next(queues.Length);
        await producer!.PostAsync(new Message
        {
            Id = Guid.NewGuid(),
            Queue = queues[index],
            Content = $"Processador pela máquina {Environment.MachineName}!",
            Delay = request.Delay,
            ForceRetry = i == indexToThrow,
        }, stoppingToken);
    }
    return HttpStatusCode.Created;
}).WithName("SubmitJob").WithOpenApi();

await app.RunAsync();