namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("UpdatePluginResponse")]
public record UpdatePluginResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("PluginType")]
    public PluginType? Plugin { get; init; }
}
