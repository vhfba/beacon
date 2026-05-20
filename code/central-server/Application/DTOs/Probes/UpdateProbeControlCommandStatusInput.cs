namespace CentralServer.Application.DTOs;

using CentralServer.Domain.Models;

public record UpdateProbeControlCommandStatusInput
{
    public string ProbeId { get; init; } = string.Empty;
    public string CommandId { get; init; } = string.Empty;
    public ProbeControlCommandStatus Status { get; init; }
    public string? ResultJson { get; init; }
    public string? ErrorMessage { get; init; }
}
