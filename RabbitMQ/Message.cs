namespace HangfireDemo.Api.RabbitMQ;

public record Message
{
    public required int Id { get; init; }
    public required string Content { get; init; }
}