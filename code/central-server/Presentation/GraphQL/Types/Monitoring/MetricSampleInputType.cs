namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
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

    public MetricSampleInput ToDTO()
    {
        return new MetricSampleInput
        {
            Name = Name,
            Kind = Kind,
            Value = Value,
            TimestampUtc = TimestampUtc,
            Labels = Labels
                .Where(label => !string.IsNullOrWhiteSpace(label.Key))
                .ToDictionary(label => label.Key, label => label.Value ?? string.Empty, StringComparer.Ordinal)
        };
    }
}
