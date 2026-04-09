namespace CentralServer.Presentation.GraphQL.Security;

using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using Microsoft.Extensions.Options;

public sealed class GraphQLRequestHardeningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<GraphQLSecurityOptions> _securityOptions;

    public GraphQLRequestHardeningMiddleware(
        RequestDelegate next,
        IOptionsMonitor<GraphQLSecurityOptions> securityOptions)
    {
        _next = next;
        _securityOptions = securityOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsGraphQLEndpoint(context.Request))
        {
            await _next(context);
            return;
        }

        var queryText = await TryReadQueryAsync(context.Request, context.RequestAborted);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            await _next(context);
            return;
        }

        var security = _securityOptions.CurrentValue;
        if (!TryParse(queryText, out var document, out var syntaxError))
        {
            await WriteGraphQLErrorAsync(context, StatusCodes.Status400BadRequest, $"Invalid GraphQL syntax: {syntaxError}");
            return;
        }

        var metrics = Analyze(document!);

        if (!security.EnableIntrospection && metrics.ContainsIntrospection)
        {
            await WriteGraphQLErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                "GraphQL introspection is disabled for this environment.");
            return;
        }

        if (security.MaxQueryDepth > 0 && metrics.MaxDepth > security.MaxQueryDepth)
        {
            await WriteGraphQLErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                $"GraphQL query depth {metrics.MaxDepth} exceeds limit {security.MaxQueryDepth}.");
            return;
        }

        if (security.MaxQueryComplexity > 0 && metrics.FieldCount > security.MaxQueryComplexity)
        {
            await WriteGraphQLErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                $"GraphQL query complexity {metrics.FieldCount} exceeds limit {security.MaxQueryComplexity}.");
            return;
        }

        await _next(context);
    }

    private static bool IsGraphQLEndpoint(HttpRequest request)
    {
        if (!request.Path.Equals("/graphql", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HttpMethods.IsPost(request.Method) || HttpMethods.IsGet(request.Method);
    }

    private static async Task<string?> TryReadQueryAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (HttpMethods.IsGet(request.Method))
        {
            var queryParam = request.Query["query"].ToString();
            if (!string.IsNullOrWhiteSpace(queryParam))
            {
                return queryParam;
            }

            return null;
        }

        if (!HttpMethods.IsPost(request.Method))
        {
            return null;
        }

        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var payload = JsonDocument.Parse(body);
            if (!payload.RootElement.TryGetProperty("query", out var queryNode) || queryNode.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var payloadQuery = queryNode.GetString();
            if (string.IsNullOrWhiteSpace(payloadQuery))
            {
                return null;
            }

            return payloadQuery;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryParse(string query, out DocumentNode? document, out string? syntaxError)
    {
        try
        {
            document = Utf8GraphQLParser.Parse(query);
            syntaxError = null;
            return true;
        }
        catch (SyntaxException ex)
        {
            document = null;
            syntaxError = ex.Message;
            return false;
        }
    }

    private static GraphQLMetrics Analyze(DocumentNode document)
    {
        var metrics = new GraphQLMetrics();
        var fragments = document.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToDictionary(fragment => fragment.Name.Value, StringComparer.Ordinal);

        foreach (var operation in document.Definitions.OfType<OperationDefinitionNode>())
        {
            AnalyzeSelectionSet(operation.SelectionSet, 1, metrics, fragments, new HashSet<string>(StringComparer.Ordinal));
        }

        return metrics;
    }

    private static void AnalyzeSelectionSet(
        SelectionSetNode selectionSet,
        int depth,
        GraphQLMetrics metrics,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        HashSet<string> recursionGuard)
    {
        metrics.MaxDepth = Math.Max(metrics.MaxDepth, depth);

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    metrics.FieldCount++;

                    if (field.Name.Value.StartsWith("__", StringComparison.Ordinal))
                    {
                        metrics.ContainsIntrospection = true;
                    }

                    if (field.SelectionSet != null)
                    {
                        AnalyzeSelectionSet(field.SelectionSet, depth + 1, metrics, fragments, recursionGuard);
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    AnalyzeSelectionSet(inlineFragment.SelectionSet, depth + 1, metrics, fragments, recursionGuard);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    var fragmentName = fragmentSpread.Name.Value;
                    if (!recursionGuard.Add(fragmentName))
                    {
                        break;
                    }

                    if (fragments.TryGetValue(fragmentName, out var fragmentDefinition))
                    {
                        AnalyzeSelectionSet(fragmentDefinition.SelectionSet, depth + 1, metrics, fragments, recursionGuard);
                    }

                    recursionGuard.Remove(fragmentName);
                    break;
            }
        }
    }

    private static async Task WriteGraphQLErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new
        {
            errors = new[]
            {
                new { message }
            }
        });

        await context.Response.WriteAsync(response);
    }

    private sealed class GraphQLMetrics
    {
        public bool ContainsIntrospection { get; set; }
        public int MaxDepth { get; set; }
        public int FieldCount { get; set; }
    }
}
