namespace CentralServer.Application.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;

public static partial class ApplicationDtoMappings
{
    public static ProbeActionExecutionDTO ToDto(this ProbeActionExecution execution)
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
