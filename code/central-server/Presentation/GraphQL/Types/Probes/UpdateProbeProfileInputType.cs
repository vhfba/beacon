namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record UpdateProbeProfileInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Location { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string RequestedBy { get; init; } = string.Empty;
}
