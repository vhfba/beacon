namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public record RegisterProbeInputType
{
    [GraphQLType("String!")]
    public string Id { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Location { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string IpAddress { get; init; } = string.Empty;

    public RegisterProbeInput ToDTO()
    {
        return new RegisterProbeInput
        {
            Id = Id,
            Name = Name,
            Location = Location,
            IpAddress = IpAddress
        };
    }
}
