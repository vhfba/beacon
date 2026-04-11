namespace CentralServer.Presentation.GraphQL.Security;

public sealed class GraphQLSecurityOptions
{
    public const string SectionName = "GraphQL";

    public bool EnableIntrospection { get; init; }
    public int MaxQueryDepth { get; init; } = 15;
    public int MaxQueryComplexity { get; init; } = 1000;
}
