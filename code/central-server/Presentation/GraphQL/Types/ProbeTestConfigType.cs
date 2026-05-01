namespace CentralServer.Presentation.GraphQL.Types;

using CentralServer.Application.DTOs;
using HotChocolate;
public class ProbeTestConfigType
{
    [GraphQLType("String!")]
    public string ProbeId { get; set; } = string.Empty;

    [GraphQLType("String!")]
    public string TestType { get; set; } = string.Empty;

    [GraphQLType("Int!")]
    public int IntervalSeconds { get; set; }

    [GraphQLType("Boolean!")]
    public bool Enabled { get; set; }
}
