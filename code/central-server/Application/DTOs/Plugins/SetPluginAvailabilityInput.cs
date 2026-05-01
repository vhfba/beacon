namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record SetPluginAvailabilityInput
{
    [Required(ErrorMessage = "Plugin ID is required")]
    public string PluginId { get; init; } = string.Empty;

    public bool Available { get; init; }
}
