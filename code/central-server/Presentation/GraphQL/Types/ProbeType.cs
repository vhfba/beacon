namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public class ProbeType
{
    [GraphQLType("String!")]
    public string Id { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Name { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string Location { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string IpAddress { get; set; } = string.Empty;

    public ProbeStatusType Status { get; set; } = ProbeStatusType.Registered;

    [GraphQLType("DateTime!")]
    public DateTime CreatedAt { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? LastHeartbeat { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? LastConfigFetch { get; set; }

    public static ProbeType FromDTO(ProbeDTO dto)
    {
        var status = Enum.TryParse<ProbeStatusType>(dto.Status, true, out var parsedStatus)
            ? parsedStatus
            : ProbeStatusType.Registered;

        return new ProbeType
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            IpAddress = dto.IpAddress,
            Status = status,
            CreatedAt = dto.CreatedAt,
            LastHeartbeat = dto.LastHeartbeat,
            LastConfigFetch = dto.LastConfigFetch
        };
    }
}
