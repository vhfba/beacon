namespace CentralServer.Presentation.GraphQL.Security;

using System.Text.Json;
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

        var queryText = await GraphQLRequestReader.TryReadQueryAsync(context.Request, context.RequestAborted);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            await _next(context);
            return;
        }

        var security = _securityOptions.CurrentValue;
        if (!GraphQLDocumentAnalyzer.TryAnalyze(queryText, out var metrics, out var syntaxError))
        {
            await WriteGraphQLErrorAsync(context, StatusCodes.Status400BadRequest, $"Invalid GraphQL syntax: {syntaxError}");
            return;
        }

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
}
