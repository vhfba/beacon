namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record RequestWifiScanInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string RequestedBy { get; init; } = string.Empty;
}
