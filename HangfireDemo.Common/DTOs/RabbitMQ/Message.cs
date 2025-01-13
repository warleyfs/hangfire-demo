namespace HangfireDemo.Contracts.DTOs.RabbitMQ;

public record Message
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required string Queue { get; init; }
    public required TimeSpan Delay { get; init; }
    public required bool ForceRetry { get; init; }
}