namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record MetricSampleInputType
{
    [GraphQLType("String!")]
    public string Name { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Kind { get; init; } = "gauge";

    [GraphQLType("Float!")]
    public double Value { get; init; }

    [GraphQLType("DateTime")]
    public DateTimeOffset? TimestampUtc { get; init; }

    public List<MetricLabelInputType> Labels { get; init; } = [];
}
