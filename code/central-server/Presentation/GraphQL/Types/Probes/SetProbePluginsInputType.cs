namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record SetProbePluginsInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("[String!]!")]
    public List<string> PluginIds { get; init; } = [];
}
