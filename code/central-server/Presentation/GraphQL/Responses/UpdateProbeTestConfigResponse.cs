namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("UpdateProbeTestConfigResponse")]
public record UpdateProbeTestConfigResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("ProbeTestConfigType")]
    public ProbeTestConfigType? Config { get; init; }
}
