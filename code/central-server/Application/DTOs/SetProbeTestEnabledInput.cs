namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record SetProbeTestEnabledInput
{
    [Required(ErrorMessage = "Probe ID is required")]
    public string ProbeId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Test type is required")]
    public string TestType { get; init; } = string.Empty;

    public bool Enabled { get; init; }
}
