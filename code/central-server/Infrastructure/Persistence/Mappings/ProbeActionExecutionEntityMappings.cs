namespace CentralServer.Infrastructure.Persistence.Mappings;

using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence.Entities;

public static class ProbeActionExecutionEntityMappings
{
    public static ProbeActionExecutionEntity ToEntity(this ProbeActionExecution execution)
    {
        return new ProbeActionExecutionEntity
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

    public static void ApplyToEntity(this ProbeActionExecution execution, ProbeActionExecutionEntity entity)
    {
        entity.Status = execution.Status;
        entity.DeliveredAtUtc = execution.DeliveredAtUtc;
        entity.StartedAtUtc = execution.StartedAtUtc;
        entity.CompletedAtUtc = execution.CompletedAtUtc;
        entity.ErrorMessage = execution.ErrorMessage;
    }

    public static ProbeActionExecution ToDomain(this ProbeActionExecutionEntity entity)
    {
        return ProbeActionExecution.Rehydrate(
            entity.ExecutionId,
            new ProbeId(entity.ProbeId),
            entity.PluginId,
            entity.TriggeredBy,
            entity.Status,
            entity.RequestedAtUtc,
            entity.DeliveredAtUtc,
            entity.StartedAtUtc,
            entity.CompletedAtUtc,
            entity.ErrorMessage);
    }
}
