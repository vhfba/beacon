namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public record SetPluginAvailabilityInputType
{
    [GraphQLType("String!")]
    public string PluginId { get; init; } = string.Empty;

    [GraphQLType("Boolean!")]
    public bool Available { get; init; }

    public SetPluginAvailabilityInput ToDTO()
    {
        return new SetPluginAvailabilityInput
        {
            PluginId = PluginId,
            Available = Available
        };
    }
}
