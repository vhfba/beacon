namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public class ProbePluginAssignmentType
{
    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string PluginId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string PluginName { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string PluginVersion { get; set; } = string.Empty;

    [GraphQLType("Boolean!")]
    public bool PluginAvailable { get; set; }

    [GraphQLType("DateTime!")]
    public DateTime AssignedAt { get; set; }
}
