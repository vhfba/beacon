namespace CentralServer.Presentation.GraphQL.Responses;

using HotChocolate;

[GraphQLName("DeleteProbeResponse")]
public record DeleteProbeResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("String")]
    public string? ProbeId { get; init; }
}
