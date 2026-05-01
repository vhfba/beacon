namespace CentralServer.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CentralServer.Domain.Models;

public class HealthAndSecurityIntegrationTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public HealthAndSecurityIntegrationTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("healthy", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GraphQL_WithoutApiKey_IsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "query { probeRuntime(probeId: \"probe-missing\") { probeId } }"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProbeRuntime_WithProbeKeyAndMissingProbe_ReturnsGraphQLError()
    {
        var client = _factory.CreateProbeClient();

        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = "query { probeRuntime(probeId: \"probe-missing\") { probeId } }"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GraphQLIntrospectionQuery_WhenDisabled_ReturnsBadRequest()
    {
        var client = _factory.CreateAdminClient();

        var requestBody = JsonSerializer.Serialize(new
        {
            query = "query { __schema { queryType { name } } }"
        });

        using var response = await client.PostAsync("/graphql", new StringContent(requestBody, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Contains("introspection is disabled", responseText, StringComparison.OrdinalIgnoreCase);
    }
}
