namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using HotChocolate;

public record UpdateProbeActionStatusInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string ExecutionId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string Status { get; init; } = string.Empty;

    [GraphQLType("String")]
    public string? ErrorMessage { get; init; }

    public UpdateProbeActionStatusInput ToDTO()
    {
        var normalizedStatus = Status.Replace("_", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<ProbeActionExecutionStatus>(normalizedStatus, true, out var parsedStatus))
        {
            throw new DomainException($"Invalid action status '{Status}'.");
        }

        return new UpdateProbeActionStatusInput
        {
            ProbeId = ProbeId,
            ExecutionId = ExecutionId,
            Status = parsedStatus,
            ErrorMessage = ErrorMessage
        };
    }
}
