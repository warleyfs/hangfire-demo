namespace HangfireDemo.Api.DTOs;

public struct SubmitJobRequest()
{
    public required int JobCount { get; init; } = 1;
    public required TimeSpan Delay { get; init; } = default;
    public required bool ForceRetry { get; init; } = false;
}