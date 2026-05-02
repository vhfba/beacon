namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record TriggerProbeActionInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string PluginId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string TriggeredBy { get; init; } = string.Empty;
}
