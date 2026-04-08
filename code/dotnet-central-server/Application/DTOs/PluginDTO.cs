namespace CentralServer.Application.DTOs;
public record PluginDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime ReleasedAt { get; init; }
    public bool Available { get; init; }
}
