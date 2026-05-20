namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record RequestWifiConnectInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Ssid { get; init; } = string.Empty;

    [GraphQLType("String")]
    public string? Password { get; init; }

    [GraphQLType("String!")]
    public string RequestedBy { get; init; } = string.Empty;
}
