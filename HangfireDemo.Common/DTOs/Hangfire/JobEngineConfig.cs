namespace HangfireDemo.Contracts.DTOs.Hangfire;

public static class JobEngineConfig
{
    public static string[] Queues => [ "queue-1", "queue-2", "queue-3", "queue-4" ];
}