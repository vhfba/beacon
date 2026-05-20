namespace CentralServer.Presentation.GraphQL.Types;

public enum ProbeControlCommandStatusType
{
    Queued,
    Delivered,
    Running,
    Succeeded,
    Failed,
    TimedOut
}
