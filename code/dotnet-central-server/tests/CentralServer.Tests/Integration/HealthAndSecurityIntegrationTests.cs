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
    public async Task RuntimeState_WithoutApiKey_IsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/probes/probe-missing/runtime-state");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RuntimeState_WithProbeKeyAndMissingProbe_ReturnsNotFound()
    {
        var client = _factory.CreateProbeClient();

        var response = await client.GetAsync("/probes/probe-missing/runtime-state");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ServiceDiscovery_WithValidToken_ReturnsOnlyActiveProbes()
    {
        await _factory.SeedProbeAsync("probe-active", "10.2.0.1", ProbeStatus.Active);
        await _factory.SeedProbeAsync("probe-registered", "10.2.0.2", ProbeStatus.Registered);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/monitoring/prometheus/service-discovery?token={_factory.ServiceDiscoveryToken}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        Assert.Single(content.EnumerateArray());

        var first = content.EnumerateArray().First();
        var labels = first.GetProperty("labels");
        Assert.Equal("probe-active", labels.GetProperty("probe_id").GetString());
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
