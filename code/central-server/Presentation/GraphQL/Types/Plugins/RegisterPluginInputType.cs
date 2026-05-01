namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using HotChocolate;

public record RegisterPluginInputType
{
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

    public RegisterPluginInput ToDTO()
    {
        return new RegisterPluginInput
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Checksum = Checksum,
            Description = Description,
            BundleDownloadUrl = BundleDownloadUrl,
            DashboardJson = DashboardJson,
            ExecutionMode = ExecutionMode == PluginExecutionModeType.Action
                ? PluginExecutionMode.Action
                : PluginExecutionMode.Scheduled
        };
    }
}
