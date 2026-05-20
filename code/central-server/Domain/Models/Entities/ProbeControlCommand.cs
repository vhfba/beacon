namespace CentralServer.Domain.Models;

public class ProbeControlCommand
{
    public string CommandId { get; private set; }
    public ProbeId ProbeId { get; private set; }
    public ProbeControlCommandType Type { get; private set; }
    public ProbeControlCommandStatus Status { get; private set; }
    public string RequestedBy { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? PayloadJson { get; private set; }
    public string? ResultJson { get; private set; }
    public string? ErrorMessage { get; private set; }

    public ProbeControlCommand(
        ProbeId probeId,
        ProbeControlCommandType type,
        string requestedBy,
        string? payloadJson = null,
        string? commandId = null)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new DomainException("RequestedBy cannot be empty for a probe control command");

        CommandId = string.IsNullOrWhiteSpace(commandId) ? Guid.NewGuid().ToString("N") : commandId.Trim();
        ProbeId = probeId;
        Type = type;
        RequestedBy = requestedBy.Trim();
        PayloadJson = NormalizeOptional(payloadJson);
        Status = ProbeControlCommandStatus.Queued;
        RequestedAtUtc = DateTime.UtcNow;
    }

    public static ProbeControlCommand Rehydrate(
        string commandId,
        ProbeId probeId,
        ProbeControlCommandType type,
        ProbeControlCommandStatus status,
        string requestedBy,
        DateTime requestedAtUtc,
        DateTime? deliveredAtUtc,
        DateTime? startedAtUtc,
        DateTime? completedAtUtc,
        string? payloadJson,
        string? resultJson,
        string? errorMessage)
    {
        return new ProbeControlCommand(probeId, type, requestedBy, payloadJson, commandId)
        {
            Status = status,
            RequestedAtUtc = requestedAtUtc,
            DeliveredAtUtc = deliveredAtUtc,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            ResultJson = NormalizeOptional(resultJson),
            ErrorMessage = NormalizeOptional(errorMessage)
        };
    }

    public void MarkDelivered(DateTime utcNow)
    {
        EnsureStatus(ProbeControlCommandStatus.Queued);
        Status = ProbeControlCommandStatus.Delivered;
        DeliveredAtUtc = utcNow;
    }

    public void MarkRunning(DateTime utcNow)
    {
        if (Status is not ProbeControlCommandStatus.Queued and not ProbeControlCommandStatus.Delivered)
            throw new DomainException($"Cannot mark command {CommandId} as running from status {Status}");

        Status = ProbeControlCommandStatus.Running;
        StartedAtUtc = utcNow;
        ErrorMessage = null;
    }

    public void MarkSucceeded(DateTime utcNow, string? resultJson)
    {
        EnsureStatus(ProbeControlCommandStatus.Running);
        Status = ProbeControlCommandStatus.Succeeded;
        CompletedAtUtc = utcNow;
        ResultJson = NormalizeOptional(resultJson);
        ErrorMessage = null;
    }

    public void MarkFailed(DateTime utcNow, string? errorMessage, bool timedOut = false)
    {
        EnsureStatus(ProbeControlCommandStatus.Running);
        Status = timedOut ? ProbeControlCommandStatus.TimedOut : ProbeControlCommandStatus.Failed;
        CompletedAtUtc = utcNow;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
            ? (timedOut ? "Probe control command timed out" : "Probe control command failed")
            : errorMessage.Trim();
    }

    private void EnsureStatus(ProbeControlCommandStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Expected status {expected} but found {Status} for probe control command {CommandId}");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
