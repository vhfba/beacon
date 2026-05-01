namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;

public class ProbeActionExecutionType
{
    [GraphQLType("String!")]
    public string ExecutionId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string PluginId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string TriggeredBy { get; set; } = string.Empty;

    [GraphQLType("ProbeActionExecutionStatusType!")]
    public ProbeActionExecutionStatusType Status { get; set; }

    [GraphQLType("DateTime!")]
    public DateTime RequestedAtUtc { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? DeliveredAtUtc { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? StartedAtUtc { get; set; }

    [GraphQLType("DateTime")]
    public DateTime? CompletedAtUtc { get; set; }

    [GraphQLType("String")]
    public string? ErrorMessage { get; set; }
}
