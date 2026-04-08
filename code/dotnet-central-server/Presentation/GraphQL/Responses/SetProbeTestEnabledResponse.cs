namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("SetProbeTestEnabledResponse")]
public record SetProbeTestEnabledResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("ProbeTestConfigType")]
    public ProbeTestConfigType? Config { get; init; }
}
