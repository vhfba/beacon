namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public class ProbeConfigType
{
    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("[ProbeTestConfigType!]!")]
    public List<ProbeTestConfigType> EnabledTests { get; set; } = [];

    [GraphQLType("[PluginType!]!")]
    public List<PluginType> AvailablePlugins { get; set; } = [];
}
