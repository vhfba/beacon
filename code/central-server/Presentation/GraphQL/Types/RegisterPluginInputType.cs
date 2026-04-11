namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
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

    public RegisterPluginInput ToDTO()
    {
        return new RegisterPluginInput
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Checksum = Checksum,
            Description = Description
        };
    }
}
