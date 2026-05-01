namespace CentralServer.Presentation.GraphQL.Responses;

using HotChocolate;

[GraphQLName("DeletePluginResponse")]
public record DeletePluginResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("String")]
    public string? PluginId { get; init; }
}
