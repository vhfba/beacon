namespace CentralServer.Tests.Integration.BlackBox;

using CentralServer.Domain.Models;
using CentralServer.Tests.Integration;

public class PluginRuntimeEligibilityFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public PluginRuntimeEligibilityFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProbeConfig_DoesNotExposeEnabledTestUntilPluginIsAssigned()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-config-unassigned";
        await _factory.SeedProbeAsync(probeId, "10.31.0.1", ProbeStatus.Active);
        await RegisterScheduledPluginAsync(admin, "UNASSIGNED_HTTP");

        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: UpdateProbeTestConfigInputTypeInput!) {
              updateProbeTestConfig(input: $input) { success config { testType enabled } }
            }
            """, new { input = new { probeId, testType = "UNASSIGNED_HTTP", intervalSeconds = 30, enabled = true } });

        var configPayload = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              probeConfig(probeId: $probeId) {
                enabledTests { testType }
                availablePlugins { id }
              }
            }
            """, new { probeId });

        var config = configPayload.GetProperty("data").GetProperty("probeConfig");
        Assert.Empty(config.GetProperty("enabledTests").EnumerateArray());
        Assert.Empty(config.GetProperty("availablePlugins").EnumerateArray());
    }

    [Fact]
    public async Task ProbeConfig_DoesNotExposeAssignedPluginWhenPluginIsUnavailable()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-config-unavailable";
        await _factory.SeedProbeAsync(probeId, "10.31.0.2", ProbeStatus.Active);
        await RegisterScheduledPluginAsync(admin, "UNAVAILABLE_HTTP");
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: SetProbePluginsInputTypeInput!) {
              setProbePlugins(input: $input) { success assignments { pluginId } }
            }
            """, new { input = new { probeId, pluginIds = new[] { "UNAVAILABLE_HTTP" } } });
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: UpdateProbeTestConfigInputTypeInput!) {
              updateProbeTestConfig(input: $input) { success config { testType enabled } }
            }
            """, new { input = new { probeId, testType = "UNAVAILABLE_HTTP", intervalSeconds = 30, enabled = true } });
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: SetPluginAvailabilityInputTypeInput!) {
              setPluginAvailability(input: $input) { success plugin { id available } }
            }
            """, new { input = new { pluginId = "UNAVAILABLE_HTTP", available = false } });

        var configPayload = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              probeConfig(probeId: $probeId) {
                enabledTests { testType }
                availablePlugins { id }
              }
            }
            """, new { probeId });

        var config = configPayload.GetProperty("data").GetProperty("probeConfig");
        Assert.Contains(config.GetProperty("enabledTests").EnumerateArray(), item => item.GetProperty("testType").GetString() == "UNAVAILABLE_HTTP");
        Assert.Empty(config.GetProperty("availablePlugins").EnumerateArray());
    }

    private static Task RegisterScheduledPluginAsync(HttpClient admin, string pluginId)
    {
        return IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: RegisterPluginInputTypeInput!) {
              registerPlugin(input: $input) { success plugin { id } }
            }
            """, new
        {
            input = new
            {
                id = pluginId,
                name = pluginId,
                version = "1.0.0",
                checksum = $"checksum-{pluginId}",
                executionMode = "SCHEDULED"
            }
        });
    }
}
