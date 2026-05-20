namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record RequestWifiConnectInput
{
    [Required]
    public string ProbeId { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Ssid { get; init; } = string.Empty;

    [StringLength(256)]
    public string? Password { get; init; }

    [Required]
    [StringLength(100)]
    public string RequestedBy { get; init; } = string.Empty;
}
