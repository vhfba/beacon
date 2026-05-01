namespace CentralServer.Domain.Models;

public class ProbeActionExecution
{
    public string ExecutionId { get; private set; }
    public ProbeId ProbeId { get; private set; }
    public string PluginId { get; private set; }
    public string TriggeredBy { get; private set; }
    public ProbeActionExecutionStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    public ProbeActionExecution(
        ProbeId probeId,
        string pluginId,
        string triggeredBy,
        string? executionId = null)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new DomainException("Plugin ID cannot be empty for an action execution");

        if (string.IsNullOrWhiteSpace(triggeredBy))
            throw new DomainException("TriggeredBy cannot be empty for an action execution");

        ExecutionId = string.IsNullOrWhiteSpace(executionId) ? Guid.NewGuid().ToString("N") : executionId.Trim();
        ProbeId = probeId;
        PluginId = pluginId.Trim();
        TriggeredBy = triggeredBy.Trim();
        Status = ProbeActionExecutionStatus.Queued;
        RequestedAtUtc = DateTime.UtcNow;
    }

    public static ProbeActionExecution Rehydrate(
        string executionId,
        ProbeId probeId,
        string pluginId,
        string triggeredBy,
        ProbeActionExecutionStatus status,
        DateTime requestedAtUtc,
        DateTime? deliveredAtUtc,
        DateTime? startedAtUtc,
        DateTime? completedAtUtc,
        string? errorMessage)
    {
        var execution = new ProbeActionExecution(probeId, pluginId, triggeredBy, executionId)
        {
            Status = status,
            RequestedAtUtc = requestedAtUtc,
            DeliveredAtUtc = deliveredAtUtc,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim()
        };

        return execution;
    }

    public void MarkDelivered(DateTime utcNow)
    {
        EnsureStatus(ProbeActionExecutionStatus.Queued);
        Status = ProbeActionExecutionStatus.Delivered;
        DeliveredAtUtc = utcNow;
    }

    public void MarkRunning(DateTime utcNow)
    {
        if (Status is not ProbeActionExecutionStatus.Queued and not ProbeActionExecutionStatus.Delivered)
            throw new DomainException($"Cannot mark execution {ExecutionId} as running from status {Status}");

        Status = ProbeActionExecutionStatus.Running;
        StartedAtUtc = utcNow;
        ErrorMessage = null;
    }

    public void MarkSucceeded(DateTime utcNow)
    {
        EnsureStatus(ProbeActionExecutionStatus.Running);
        Status = ProbeActionExecutionStatus.Succeeded;
        CompletedAtUtc = utcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(DateTime utcNow, string? errorMessage)
    {
        EnsureStatus(ProbeActionExecutionStatus.Running);
        Status = ProbeActionExecutionStatus.Failed;
        CompletedAtUtc = utcNow;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Action failed" : errorMessage.Trim();
    }

    public void MarkTimedOut(DateTime utcNow, string? errorMessage)
    {
        EnsureStatus(ProbeActionExecutionStatus.Running);
        Status = ProbeActionExecutionStatus.TimedOut;
        CompletedAtUtc = utcNow;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Action timed out" : errorMessage.Trim();
    }

    private void EnsureStatus(ProbeActionExecutionStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Expected status {expected} but found {Status} for action execution {ExecutionId}");
    }
}