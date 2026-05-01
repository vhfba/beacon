namespace CentralServer.Application.DTOs;

using CentralServer.Domain.Models;

public record ProbeActionExecutionDTO
{
    public string ExecutionId { get; init; } = string.Empty;
    public string ProbeId { get; init; } = string.Empty;
    public string PluginId { get; init; } = string.Empty;
    public string TriggeredBy { get; init; } = string.Empty;
    public ProbeActionExecutionStatus Status { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }

    public static ProbeActionExecutionDTO FromDomain(ProbeActionExecution execution)
    {
        return new ProbeActionExecutionDTO
        {
            ExecutionId = execution.ExecutionId,
            ProbeId = execution.ProbeId.Value,
            PluginId = execution.PluginId,
            TriggeredBy = execution.TriggeredBy,
            Status = execution.Status,
            RequestedAtUtc = execution.RequestedAtUtc,
            DeliveredAtUtc = execution.DeliveredAtUtc,
            StartedAtUtc = execution.StartedAtUtc,
            CompletedAtUtc = execution.CompletedAtUtc,
            ErrorMessage = execution.ErrorMessage
        };
    }
}
