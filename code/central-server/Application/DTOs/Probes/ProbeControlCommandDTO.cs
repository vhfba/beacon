namespace CentralServer.Application.DTOs;

using CentralServer.Domain.Models;

public record ProbeControlCommandDTO
{
    public string CommandId { get; init; } = string.Empty;
    public string ProbeId { get; init; } = string.Empty;
    public ProbeControlCommandType Type { get; init; }
    public ProbeControlCommandStatus Status { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? PayloadJson { get; init; }
    public string? ResultJson { get; init; }
    public string? ErrorMessage { get; init; }
}
