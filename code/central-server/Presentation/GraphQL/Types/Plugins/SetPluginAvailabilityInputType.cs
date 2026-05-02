namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record SetPluginAvailabilityInputType
{
    [GraphQLType("String!")]
    public string PluginId { get; init; } = string.Empty;

    [GraphQLType("Boolean!")]
    public bool Available { get; init; }
}
