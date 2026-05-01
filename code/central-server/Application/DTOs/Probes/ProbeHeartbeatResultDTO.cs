namespace CentralServer.Application.DTOs;

public record ProbeHeartbeatResultDTO
{
    public bool AutoRegistered { get; init; }
    public ProbeDTO Probe { get; init; } = new();
    public ProbeRuntimeDTO Runtime { get; init; } = new();
}
