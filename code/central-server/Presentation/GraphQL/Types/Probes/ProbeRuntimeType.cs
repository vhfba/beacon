namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public class ProbeRuntimeType
{
    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Status { get; set; } = string.Empty;

    [GraphQLType("Boolean!")]
    public bool CanEmitMetrics { get; set; }

    [GraphQLType("[String!]!")]
    public IReadOnlyList<string> EnabledTests { get; set; } = [];

    [GraphQLType("String!")]
    public string Site { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string IpAddress { get; set; } = string.Empty;

    [GraphQLType("DateTime!")]
    public DateTimeOffset PolledAtUtc { get; set; }
}
