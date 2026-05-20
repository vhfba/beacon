namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record UpdateProbeControlCommandStatusInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string CommandId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Status { get; init; } = string.Empty;

    [GraphQLType("String")]
    public string? ResultJson { get; init; }

    [GraphQLType("String")]
    public string? ErrorMessage { get; init; }
}
