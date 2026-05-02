namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record ProbeHeartbeatInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Location { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string IpAddress { get; init; } = string.Empty;

    [GraphQLType("String")]
    public string? Ssid { get; init; }

    [GraphQLType("String")]
    public string? AgentVersion { get; init; }

}
