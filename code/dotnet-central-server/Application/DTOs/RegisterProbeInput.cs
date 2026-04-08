namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;
public record RegisterProbeInput
{
    [Required(ErrorMessage = "Probe ID is required")]
    [StringLength(50, ErrorMessage = "Probe ID cannot exceed 50 characters")]
    public string Id { get; init; } = string.Empty;

    [Required(ErrorMessage = "Probe name is required")]
    [StringLength(255, ErrorMessage = "Probe name cannot exceed 255 characters")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
    public string Location { get; init; } = string.Empty;

    [Required(ErrorMessage = "IP address is required")]
    [StringLength(45, ErrorMessage = "IP address cannot exceed 45 characters")]
    public string IpAddress { get; init; } = string.Empty;
}
