namespace CentralServer.Application.Mappings;

using System.Text.Json.Nodes;
using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;

public static partial class ApplicationDtoMappings
{
    public static ProbeControlCommandDTO ToDto(this ProbeControlCommand command, bool redactSensitivePayload = false)
    {
        return new ProbeControlCommandDTO
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
            PayloadJson = redactSensitivePayload ? RedactSensitivePayload(command.PayloadJson) : command.PayloadJson,
            ResultJson = command.ResultJson,
            ErrorMessage = command.ErrorMessage
        };
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"""password""\s*:\s*""[^""]*""", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex PasswordPropertyRegex();

    public static string? RedactSensitivePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return payloadJson;

        return PasswordPropertyRegex().Replace(payloadJson, "\"password\":\"***\"");
    }
}
