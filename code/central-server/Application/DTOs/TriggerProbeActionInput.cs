namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record TriggerProbeActionInput
{
    [Required(ErrorMessage = "Probe ID is required")]
    public string ProbeId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Plugin ID is required")]
    public string PluginId { get; init; } = string.Empty;

    [Required(ErrorMessage = "TriggeredBy is required")]
    [StringLength(100, ErrorMessage = "TriggeredBy cannot exceed 100 characters")]
    public string TriggeredBy { get; init; } = string.Empty;
}