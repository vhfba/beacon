namespace CentralServer.Presentation.GraphQL.Responses;

using HotChocolate;

[GraphQLName("ReportProbeMetricsResponse")]
public record ReportProbeMetricsResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("Int!")]
    public int AcceptedSamples { get; init; }

    [GraphQLType("DateTime!")]
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
