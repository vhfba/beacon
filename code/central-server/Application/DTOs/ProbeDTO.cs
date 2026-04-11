namespace CentralServer.Application.DTOs;
public record ProbeDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public DateTime? LastConfigFetch { get; init; }
}
