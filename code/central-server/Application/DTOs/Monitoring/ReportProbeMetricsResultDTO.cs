namespace CentralServer.Application.DTOs;

public record ReportProbeMetricsResultDTO
{
    public string ProbeId { get; init; } = string.Empty;
    public int AcceptedSamples { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
