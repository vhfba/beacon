namespace CentralServer.Application.DTOs;

public record MetricSampleInput
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = "gauge";
    public double Value { get; init; }
    public DateTimeOffset? TimestampUtc { get; init; }
    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}
