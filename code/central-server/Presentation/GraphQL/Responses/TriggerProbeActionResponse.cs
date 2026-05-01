namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;

[GraphQLName("TriggerProbeActionResponse")]
public record TriggerProbeActionResponse
{
    [GraphQLType("Boolean!")]
    public bool Success { get; init; }

    [GraphQLType("String")]
    public string? Message { get; init; }

    [GraphQLType("ProbeActionExecutionType")]
    public ProbeActionExecutionType? Execution { get; init; }
}