namespace CentralServer.Application.DTOs;

public record ProbeHeartbeatInput
{
    public string ProbeId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string? Ssid { get; init; }
    public string? AgentVersion { get; init; }
}
