namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public record ReportProbeMetricsInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    public List<MetricSampleInputType> Samples { get; init; } = [];

    public ReportProbeMetricsInput ToDTO()
    {
        return new ReportProbeMetricsInput
        {
            ProbeId = ProbeId,
            Samples = Samples.Select(sample => sample.ToDTO()).ToList()
        };
    }
}
