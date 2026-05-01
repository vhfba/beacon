namespace CentralServer.Presentation.GraphQL.Security;

internal sealed class GraphQLRequestMetrics
{
    public bool ContainsIntrospection { get; set; }
    public int MaxDepth { get; set; }
    public int FieldCount { get; set; }
}
