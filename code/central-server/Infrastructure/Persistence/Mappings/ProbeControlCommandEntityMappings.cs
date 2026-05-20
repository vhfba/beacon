namespace CentralServer.Infrastructure.Persistence.Mappings;

using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence.Entities;

public static class ProbeControlCommandEntityMappings
{
    public static ProbeControlCommandEntity ToEntity(this ProbeControlCommand command)
    {
        return new ProbeControlCommandEntity
        {
            CommandId = command.CommandId,
            ProbeId = command.ProbeId.Value,
            Type = command.Type,
            Status = command.Status,
            RequestedBy = command.RequestedBy,
            RequestedAtUtc = command.RequestedAtUtc,
            DeliveredAtUtc = command.DeliveredAtUtc,
            StartedAtUtc = command.StartedAtUtc,
            CompletedAtUtc = command.CompletedAtUtc,
            PayloadJson = command.PayloadJson,
            ResultJson = command.ResultJson,
            ErrorMessage = command.ErrorMessage
        };
    }

    public static void ApplyToEntity(this ProbeControlCommand command, ProbeControlCommandEntity entity)
    {
        entity.Status = command.Status;
        entity.DeliveredAtUtc = command.DeliveredAtUtc;
        entity.StartedAtUtc = command.StartedAtUtc;
        entity.CompletedAtUtc = command.CompletedAtUtc;
        entity.ResultJson = command.ResultJson;
        entity.ErrorMessage = command.ErrorMessage;
    }

    public static ProbeControlCommand ToDomain(this ProbeControlCommandEntity entity)
    {
        return ProbeControlCommand.Rehydrate(
            entity.CommandId,
            new ProbeId(entity.ProbeId),
            entity.Type,
            entity.Status,
            entity.RequestedBy,
            entity.RequestedAtUtc,
            entity.DeliveredAtUtc,
            entity.StartedAtUtc,
            entity.CompletedAtUtc,
            entity.PayloadJson,
            entity.ResultJson,
            entity.ErrorMessage);
    }
}
