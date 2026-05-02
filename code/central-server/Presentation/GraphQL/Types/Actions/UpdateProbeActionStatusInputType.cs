namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record UpdateProbeActionStatusInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string ExecutionId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Status { get; init; } = string.Empty;

    [GraphQLType("String")]
    public string? ErrorMessage { get; init; }
}
