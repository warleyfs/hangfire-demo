using System.Net;
using System.Net.Mime;
using HangfireDemo.Contracts.DTOs;
using HangfireDemo.Api.Workers;
using HangfireDemo.Api.Workers.RabbitMQ;
using HangfireDemo.Contracts.DTOs.Hangfire;
using HangfireDemo.Contracts.DTOs.RabbitMQ;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IProducer, MessageProducer>();
builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    busConfigurator.UsingRabbitMq((context, busFactoryConfigurator) =>
    {
        busFactoryConfigurator.Host("localhost", hostConfigurator =>
        {
            hostConfigurator.Username("guest");
            hostConfigurator.Password("guest");
        });
    });
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

app.Logger.LogInformation("Application started!");

app.UseSwagger();
app.UseSwaggerUI();

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
        var index = new Random().Next(JobEngineConfig.Queues.Length);
        await producer!.PostAsync(new Message
        {
            Id = Guid.NewGuid(),
            Queue = JobEngineConfig.Queues[index],
            Content = $"Processado pela máquina {Environment.MachineName}!",
            Delay = request.Delay,
            ForceRetry = i == indexToThrow,
        }, stoppingToken);
    }
    
    return HttpStatusCode.Created;
}).WithName("SubmitJob").WithOpenApi();

await app.RunAsync();