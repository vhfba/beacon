namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public record SetProbePluginsInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("[String!]!")]
    public List<string> PluginIds { get; init; } = [];

    public SetProbePluginsInput ToDTO()
    {
        return new SetProbePluginsInput
        {
            ProbeId = ProbeId,
            PluginIds = PluginIds
        };
    }
}
