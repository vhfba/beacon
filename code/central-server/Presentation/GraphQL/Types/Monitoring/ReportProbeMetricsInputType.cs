namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record ReportProbeMetricsInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    public List<MetricSampleInputType> Samples { get; init; } = [];
}
