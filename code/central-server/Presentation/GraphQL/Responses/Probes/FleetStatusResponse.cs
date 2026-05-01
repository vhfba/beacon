namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("FleetStatusResponse")]
public record FleetStatusResponse
{
    [GraphQLType("[ProbeType!]!")]
    public List<ProbeType> Probes { get; init; } = [];
}
