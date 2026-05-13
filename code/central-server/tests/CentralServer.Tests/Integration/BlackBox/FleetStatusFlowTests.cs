namespace CentralServer.Tests.Integration.BlackBox;

using System.Text.Json;
using CentralServer.Tests.Integration;

public class FleetStatusFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public FleetStatusFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReflectsHeartbeatConfigFetchAndMetricsFreshness()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-freshness";

        await IntegrationTestClient.PostGraphQLAsync(probe, """
            mutation($input: ProbeHeartbeatInputTypeInput!) {
              recordProbeHeartbeat(input: $input) { success }
            }
            """, new { input = IntegrationTestClient.HeartbeatInput(probeId, "10.23.0.1") });
        await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              probeConfig(probeId: $probeId) { probeId }
            }
            """, new { probeId });
        await IntegrationTestClient.ReportCoverageMetricsAsync(probe, probeId, "Building B", -72, 20, 68, 95, 3);

        var statusPayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            query {
              fleetStatus {
                probes {
                  id
                  lastHeartbeat
                  lastConfigFetch
                  lastMetricsPush
                }
              }
            }
            """);

        var probes = statusPayload.GetProperty("data").GetProperty("fleetStatus").GetProperty("probes").EnumerateArray().ToArray();
        var item = Assert.Single(probes, p => p.GetProperty("id").GetString() == probeId);
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("lastHeartbeat").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("lastConfigFetch").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("lastMetricsPush").ValueKind);
    }
}
