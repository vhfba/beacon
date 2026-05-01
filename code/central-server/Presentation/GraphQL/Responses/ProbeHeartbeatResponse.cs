namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("ProbeHeartbeatResponse")]
public record ProbeHeartbeatResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("Boolean!")]
    public bool AutoRegistered { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("ProbeType")]
    public ProbeType? Probe { get; init; }

    [GraphQLType("ProbeRuntimeType")]
    public ProbeRuntimeType? Runtime { get; init; }
}
