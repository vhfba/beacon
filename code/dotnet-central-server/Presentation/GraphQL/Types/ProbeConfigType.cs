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

    public static ProbeConfigType FromDTO(ProbeConfigDTO dto)
    {
        return new ProbeConfigType
        {
            ProbeId = dto.ProbeId,
            EnabledTests = dto.EnabledTests.Select(ProbeTestConfigType.FromDTO).ToList(),
            AvailablePlugins = dto.AvailablePlugins.Select(PluginType.FromDTO).ToList()
        };
    }
}
