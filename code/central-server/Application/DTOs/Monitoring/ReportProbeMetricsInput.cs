namespace CentralServer.Application.DTOs;

public record ReportProbeMetricsInput
{
    public string ProbeId { get; init; } = string.Empty;
    public IReadOnlyList<MetricSampleInput> Samples { get; init; } = [];
}
