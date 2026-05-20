namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record UpdateProbeProfileInput
{
    [Required]
    public string ProbeId { get; init; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Location { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string RequestedBy { get; init; } = string.Empty;
}
