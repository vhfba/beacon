namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;
public record UpdateProbeTestConfigInput
{
    [Required(ErrorMessage = "Probe ID is required")]
    public string ProbeId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Test type is required")]
    public string TestType { get; init; } = string.Empty;

    [Range(5, 3600, ErrorMessage = "Interval must be between 5 and 3600 seconds")]
    public int IntervalSeconds { get; init; }

    public bool Enabled { get; init; } = true;
}
