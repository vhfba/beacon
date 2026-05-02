namespace CentralServer.Application.DTOs;

public record ProbeCoverageSummaryDTO
{
    public string ProbeId { get; init; } = string.Empty;
    public string Site { get; init; } = string.Empty;
    public int Score { get; init; }
    public string Grade { get; init; } = "NO_DATA";
    public double? RssiDbm { get; init; }
    public double? SnrDb { get; init; }
    public double? LinkQualityPercent { get; init; }
    public double? PingLatencyMs { get; init; }
    public double? PingPacketLossPercent { get; init; }
    public int SampleCount { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
