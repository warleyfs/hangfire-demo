namespace HangfireDemo.Contracts.DTOs.Hangfire;

public static class JobEngineConfig
{
    private static string[] _queues;

    public static string[] Queues
    {
        get
        {
            return _queues;
        }
        private set
        {
            const int queuesCount = 4;
            value = new string[queuesCount];
            for (var i = 0; i < queuesCount; i++) value[i] = $"queue-{i + 1}";
            _queues = value;
        }
    }
}