namespace CentralServer.Tests.Integration.BlackBox;

using CentralServer.Tests.Integration;

public class ProbeRuntimeFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public ProbeRuntimeFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RuntimeEligibility_ReflectsStatusAndEnabledTests()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-runtime-cycle";

        await IntegrationTestClient.PostGraphQLAsync(probe, """
            mutation($input: ProbeHeartbeatInputTypeInput!) {
              recordProbeHeartbeat(input: $input) {
                success
                runtime { canEmitMetrics enabledTests status }
              }
            }
            """, new { input = IntegrationTestClient.HeartbeatInput(probeId, "10.21.0.1") });

        var registeredRuntime = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              probeRuntime(probeId: $probeId) { status canEmitMetrics enabledTests }
            }
            """, new { probeId });

        var registered = registeredRuntime.GetProperty("data").GetProperty("probeRuntime");
        Assert.Equal("REGISTERED", registered.GetProperty("status").GetString());
        Assert.False(registered.GetProperty("canEmitMetrics").GetBoolean());

        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: RegisterPluginInputTypeInput!) {
              registerPlugin(input: $input) { success plugin { id } }
            }
            """, new
        {
            input = new
            {
                id = "HTTP",
                name = "HTTP Check",
                version = "1.0.0",
                checksum = "checksum-http",
                executionMode = "SCHEDULED"
            }
        });
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: UpdateProbeTestConfigInputTypeInput!) {
              updateProbeTestConfig(input: $input) { success config { testType enabled } }
            }
            """, new { input = new { probeId, testType = "HTTP", intervalSeconds = 45, enabled = true } });
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: SetProbePluginsInputTypeInput!) {
              setProbePlugins(input: $input) { success assignments { pluginId } }
            }
            """, new { input = new { probeId, pluginIds = new[] { "HTTP" } } });
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($probeId: String!, $status: String!) {
              updateProbeStatus(probeId: $probeId, status: $status) { success probe { status } }
            }
            """, new { probeId, status = "ACTIVE" });

        var activeRuntime = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              probeRuntime(probeId: $probeId) { status canEmitMetrics enabledTests }
            }
            """, new { probeId });

        var active = activeRuntime.GetProperty("data").GetProperty("probeRuntime");
        Assert.Equal("ACTIVE", active.GetProperty("status").GetString());
        Assert.True(active.GetProperty("canEmitMetrics").GetBoolean());
        Assert.Contains(active.GetProperty("enabledTests").EnumerateArray(), item => item.GetString() == "HTTP");
    }
}
