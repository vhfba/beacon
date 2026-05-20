namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record RequestWifiScanInput
{
    [Required]
    public string ProbeId { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string RequestedBy { get; init; } = string.Empty;
}
