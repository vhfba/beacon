namespace CentralServer.Presentation.GraphQL.Types;

using HotChocolate;
public enum ProbeStatusType
{
    [GraphQLName("REGISTERED")]
    Registered,

    [GraphQLName("ACTIVE")]
    Active,

    [GraphQLName("INACTIVE")]
    Inactive,

    [GraphQLName("DECOMMISSIONED")]
    Decommissioned
}
