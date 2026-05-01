namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public enum ProbeActionExecutionStatusType
{
    [GraphQLName("QUEUED")]
    Queued,

    [GraphQLName("DELIVERED")]
    Delivered,

    [GraphQLName("RUNNING")]
    Running,

    [GraphQLName("SUCCEEDED")]
    Succeeded,

    [GraphQLName("FAILED")]
    Failed,

    [GraphQLName("TIMED_OUT")]
    TimedOut
}