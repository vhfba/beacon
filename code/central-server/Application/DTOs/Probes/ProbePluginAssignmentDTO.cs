namespace CentralServer.Application.DTOs;

public record ProbePluginAssignmentDTO
{
    public string ProbeId { get; init; } = string.Empty;
    public string PluginId { get; init; } = string.Empty;
    public string PluginName { get; init; } = string.Empty;
    public string PluginVersion { get; init; } = string.Empty;
    public bool PluginAvailable { get; init; }
    public DateTime AssignedAt { get; init; }
}
