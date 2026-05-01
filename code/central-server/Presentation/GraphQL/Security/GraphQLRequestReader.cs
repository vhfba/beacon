namespace CentralServer.Presentation.GraphQL.Security;

using System.Text;
using System.Text.Json;

internal static class GraphQLRequestReader
{
    public static async Task<string?> TryReadQueryAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (HttpMethods.IsGet(request.Method))
        {
            var queryParam = request.Query["query"].ToString();
            return string.IsNullOrWhiteSpace(queryParam) ? null : queryParam;
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

            var query = queryNode.GetString();
            return string.IsNullOrWhiteSpace(query) ? null : query;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
