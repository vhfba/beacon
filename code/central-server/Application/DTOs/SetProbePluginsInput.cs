namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record SetProbePluginsInput
{
    [Required(ErrorMessage = "Probe ID is required")]
    public string ProbeId { get; init; } = string.Empty;

    public List<string> PluginIds { get; init; } = [];
}
