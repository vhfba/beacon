namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record SetProbeTestEnabledInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string TestType { get; init; } = string.Empty;

    [GraphQLType("Boolean!")]
    public bool Enabled { get; init; }
}
