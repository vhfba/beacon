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

    public static PluginType FromDTO(PluginDTO dto)
    {
        return new PluginType
        {
            Id = dto.Id,
            Name = dto.Name,
            Version = dto.Version,
            Checksum = dto.Checksum,
            Description = dto.Description,
            ReleasedAt = dto.ReleasedAt,
            Available = dto.Available
        };
    }
}
