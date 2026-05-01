namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record MetricLabelInputType
{
    [GraphQLType("String!")]
    public string Key { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Value { get; init; } = string.Empty;
}
