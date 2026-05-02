namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public class ProbeCoverageSummaryType
{
    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Site { get; set; } = string.Empty;

    [GraphQLType("Int!")]
    public int Score { get; set; }

    [GraphQLType("String!")]
    public string Grade { get; set; } = "NO_DATA";

    [GraphQLType("Float")]
    public double? RssiDbm { get; set; }

    [GraphQLType("Float")]
    public double? SnrDb { get; set; }

    [GraphQLType("Float")]
    public double? LinkQualityPercent { get; set; }

    [GraphQLType("Float")]
    public double? PingLatencyMs { get; set; }

    [GraphQLType("Float")]
    public double? PingPacketLossPercent { get; set; }

    [GraphQLType("Int!")]
    public int SampleCount { get; set; }

    [GraphQLType("DateTime!")]
    public DateTimeOffset ReceivedAtUtc { get; set; }
}
