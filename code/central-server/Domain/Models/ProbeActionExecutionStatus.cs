namespace CentralServer.Domain.Models;

public enum ProbeActionExecutionStatus
{
    Queued = 0,
    Delivered = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    TimedOut = 5
}