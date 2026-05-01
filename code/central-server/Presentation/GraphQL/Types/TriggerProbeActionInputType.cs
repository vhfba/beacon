namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public record TriggerProbeActionInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string PluginId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string TriggeredBy { get; init; } = string.Empty;

    public TriggerProbeActionInput ToDTO()
    {
        return new TriggerProbeActionInput
        {
            ProbeId = ProbeId,
            PluginId = PluginId,
            TriggeredBy = TriggeredBy
        };
    }
}