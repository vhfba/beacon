namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public enum PluginExecutionModeType
{
    [GraphQLName("SCHEDULED")]
    Scheduled,

    [GraphQLName("ACTION")]
    Action
}
