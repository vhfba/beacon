namespace CentralServer.Domain.Models;

public enum ProbeControlCommandStatus
{
    Queued,
    Delivered,
    Running,
    Succeeded,
    Failed,
    TimedOut
}
