namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;

public class ProbeControlCommandType
{
    [GraphQLType("String!")]
    public string CommandId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("ProbeControlCommandTypeType!")]
    public ProbeControlCommandTypeType Type { get; set; }

    [GraphQLType("ProbeControlCommandStatusType!")]
    public ProbeControlCommandStatusType Status { get; set; }

    [GraphQLType("String!")]
    public string RequestedBy { get; set; } = string.Empty;

    [GraphQLType("DateTime!")]
    public DateTime RequestedAtUtc { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? DeliveredAtUtc { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? StartedAtUtc { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? CompletedAtUtc { get; set; }

    [GraphQLType("String")]
    public string? PayloadJson { get; set; }

    [GraphQLType("String")]
    public string? ResultJson { get; set; }

    [GraphQLType("String")]
    public string? ErrorMessage { get; set; }
}
