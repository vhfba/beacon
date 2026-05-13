namespace CentralServer.Tests.Integration.E2E;

using System.Net;
using CentralServer.Tests.Integration;

public class ScheduledMonitoringE2ETests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public ScheduledMonitoringE2ETests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ScheduledMonitoringFlow_ExposesConfigMetricsAndCoverage()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-e2e";

        await IntegrationTestClient.PostGraphQLAsync(probe, """
            mutation($input: ProbeHeartbeatInputTypeInput!) {
              recordProbeHeartbeat(input: $input) { success autoRegistered }
            }
            """, new { input = IntegrationTestClient.HeartbeatInput(probeId, "10.20.0.1") });

        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: RegisterPluginInputTypeInput!) {
              registerPlugin(input: $input) {
                success
                plugin { id version available executionMode bundleDownloadUrl }
              }
            }
            """, new
        {
            input = new
            {
                id = "PING",
                name = "Ping Check",
                version = "1.0.0",
                checksum = "checksum-ping",
                description = "Scheduled ping check",
                bundleDownloadUrl = "https://example.com/plugins/PING-1.0.0.zip",
                executionMode = "SCHEDULED"
            }
        });

        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($probeId: String!, $status: String!) {
              updateProbeStatus(probeId: $probeId, status: $status) {
                success
                probe { id status }
              }
            }
            """, new { probeId, status = "ACTIVE" });

        var assignmentPayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: SetProbePluginsInputTypeInput!) {
              setProbePlugins(input: $input) {
                success
                assignments { probeId pluginId pluginVersion pluginAvailable }
              }
            }
            """, new { input = new { probeId, pluginIds = new[] { "PING" } } });

        var assignments = assignmentPayload.GetProperty("data").GetProperty("setProbePlugins").GetProperty("assignments").EnumerateArray().ToArray();
        Assert.Single(assignments);
        Assert.Equal("PING", assignments[0].GetProperty("pluginId").GetString());
        Assert.True(assignments[0].GetProperty("pluginAvailable").GetBoolean());

        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: UpdateProbeTestConfigInputTypeInput!) {
              updateProbeTestConfig(input: $input) {
                success
                config { probeId testType intervalSeconds enabled }
              }
            }
            """, new { input = new { probeId, testType = "PING", intervalSeconds = 30, enabled = true } });

        var configPayload = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              probeConfig(probeId: $probeId) {
                probeId
                enabledTests { testType intervalSeconds enabled }
                availablePlugins { id version executionMode bundleDownloadUrl }
              }
            }
            """, new { probeId });

        var config = configPayload.GetProperty("data").GetProperty("probeConfig");
        Assert.Equal(probeId, config.GetProperty("probeId").GetString());
        Assert.Contains(config.GetProperty("enabledTests").EnumerateArray(), item => item.GetProperty("testType").GetString() == "PING");
        Assert.Contains(config.GetProperty("availablePlugins").EnumerateArray(), item => item.GetProperty("id").GetString() == "PING");

        await IntegrationTestClient.ReportCoverageMetricsAsync(probe, probeId, site: "Building A", rssi: -55, snr: 35, linkQuality: 94, latency: 18, loss: 0);

        var metricsResponse = await probe.GetAsync("/metrics");
        var metrics = await metricsResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        Assert.Contains("beacon_wifi_rssi_dbm", metrics, StringComparison.Ordinal);
        Assert.Contains("probe_id=\"probe-e2e\"", metrics, StringComparison.Ordinal);
        Assert.Contains("site=\"Building A\"", metrics, StringComparison.Ordinal);

        var coveragePayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            query {
              fleetCoverage {
                probeId
                site
                score
                grade
              }
            }
            """);
        var coverage = coveragePayload.GetProperty("data").GetProperty("fleetCoverage").EnumerateArray().ToArray();
        Assert.Contains(coverage, item =>
            item.GetProperty("probeId").GetString() == probeId
            && item.GetProperty("site").GetString() == "Building A"
            && item.GetProperty("grade").GetString() == "EXCELLENT");
    }
}
