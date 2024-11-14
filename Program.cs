using System.Net;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoUrlBuilder = new MongoUrlBuilder("mongodb://root:123@localhost:27017/jobs");
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
        CheckConnection = true
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard();
app.UseHttpsRedirection();

app.MapPost("/job", (int jobCount, TimeSpan delay) =>
{
    for (var i = 0; i < jobCount; i++)
    {
        var index = new Random().Next(queues.Length);
        new BackgroundJobClient().Schedule(queues[index], () => Console.WriteLine("Background job triggered"), delay);
    }
    return HttpStatusCode.Created;
}).WithName("SubmitJob").WithOpenApi();

app.Run();
