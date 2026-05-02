namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public class PluginType
{
    [GraphQLType("String!")]
    public string Id { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Version { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Checksum { get; set; } = string.Empty;

    [GraphQLType("String")]
    public string? Description { get; set; }

    [GraphQLType("DateTime!")]
    public DateTime ReleasedAt { get; set; }

    [GraphQLType("Boolean!")]
    public bool Available { get; set; }

    [GraphQLType("PluginExecutionModeType!")]
    public PluginExecutionModeType ExecutionMode { get; set; } = PluginExecutionModeType.Scheduled;

    [GraphQLType("String!")]
    public string BundleUrl { get; set; } = string.Empty;

    [GraphQLType("String")]
    public string? BundleDownloadUrl { get; set; }

    [GraphQLType("String")]
    public string? DashboardJson { get; set; }

    [GraphQLType("Boolean!")]
    public bool HasDashboard { get; set; }

    [GraphQLType("String")]
    public string? DashboardUid { get; set; }
}
