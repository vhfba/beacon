namespace CentralServer.Infrastructure.Metrics;

public sealed class MetricsStoreOptions
{
    public const string SectionName = "Metrics";

    public string Provider { get; init; } = "Redis";

    public RedisMetricsStoreOptions Redis { get; init; } = new();
}

public sealed class RedisMetricsStoreOptions
{
    public string ConnectionString { get; init; } = "localhost:6379";

    public string KeyPrefix { get; init; } = "beacon:metrics";

    public int ProbeSnapshotTtlSeconds { get; init; } = 600;
}
