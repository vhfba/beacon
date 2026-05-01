namespace CentralServer.Application.DTOs;

public record ProbeRuntimeDTO
{
    public string ProbeId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool CanEmitMetrics { get; init; }
    public string Site { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public DateTimeOffset PolledAtUtc { get; init; }
    public IReadOnlyList<string> EnabledTests { get; init; } = [];
}
