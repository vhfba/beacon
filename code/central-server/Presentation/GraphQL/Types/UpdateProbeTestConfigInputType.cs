namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public record UpdateProbeTestConfigInputType
{
    [GraphQLType("String!")]
    public string ProbeId { get; init; } = string.Empty;

    [GraphQLType("String!")]
    public string TestType { get; init; } = string.Empty;

    [GraphQLType("Int!")]
    public int IntervalSeconds { get; init; }

    [GraphQLType("Boolean!")]
    public bool Enabled { get; init; } = true;

    public UpdateProbeTestConfigInput ToDTO()
    {
        return new UpdateProbeTestConfigInput
        {
            ProbeId = ProbeId,
            TestType = TestType,
            IntervalSeconds = IntervalSeconds,
            Enabled = Enabled
        };
    }
}
