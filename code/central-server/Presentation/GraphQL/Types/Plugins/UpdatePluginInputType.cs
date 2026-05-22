namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public record UpdatePluginInputType
{
    [GraphQLType("String!")]
    public string CurrentId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Id { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Version { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Checksum { get; init; } = string.Empty;

    [GraphQLType("String")]
    public string? Description { get; init; }

    [GraphQLType("String")]
    public string? BundleDownloadUrl { get; init; }

    [GraphQLType("String")]
    public string? DashboardJson { get; init; }

    [GraphQLType("PluginExecutionModeType!")]
    public PluginExecutionModeType ExecutionMode { get; init; } = PluginExecutionModeType.Scheduled;
}
