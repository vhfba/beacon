namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("SetProbePluginsResponse")]
public record SetProbePluginsResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("[ProbePluginAssignmentType!]!")]
    public List<ProbePluginAssignmentType> Assignments { get; init; } = [];
}
