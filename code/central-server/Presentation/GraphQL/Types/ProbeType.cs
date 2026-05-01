namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public class ProbeType
{
    [GraphQLType("String!")]
    public string Id { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Location { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string IpAddress { get; set; } = string.Empty;

    [GraphQLType("String")]
    public string? Ssid { get; set; }

    [GraphQLType("String")]
    public string? AgentVersion { get; set; }

    public ProbeStatusType Status { get; set; } = ProbeStatusType.Registered;

    [GraphQLType("DateTime!")]
    public DateTime CreatedAt { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? LastHeartbeat { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? LastConfigFetch { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? LastMetricsPush { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? LastSeenAt { get; set; }
}
