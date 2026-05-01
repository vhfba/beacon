namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using CentralServer.Domain.Models;

public record UpdateProbeActionStatusInput
{
    [Required(ErrorMessage = "Probe ID is required")]
    public string ProbeId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Execution ID is required")]
    public string ExecutionId { get; init; } = string.Empty;

    public ProbeActionExecutionStatus Status { get; init; }

    [StringLength(1024, ErrorMessage = "Error message cannot exceed 1024 characters")]
    public string? ErrorMessage { get; init; }
}
