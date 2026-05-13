namespace CentralServer.Tests.Integration.BlackBox;

using CentralServer.Domain.Models;
using CentralServer.Tests.Integration;

public class ActionTriggerFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public ActionTriggerFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullLifecycle_CanBeQueuedClaimedUpdatedAndReviewed()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-action-flow";

        await _factory.SeedProbeAsync(probeId, "10.22.0.1", ProbeStatus.Active);
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: RegisterPluginInputTypeInput!) {
              registerPlugin(input: $input) { success plugin { id executionMode } }
            }
            """, new
        {
            input = new
            {
                id = "WIFI_SCAN_ACTION",
                name = "WiFi Scan",
                version = "1.0.0",
                checksum = "checksum-action",
                executionMode = "ACTION"
            }
        });
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: SetProbePluginsInputTypeInput!) {
              setProbePlugins(input: $input) { success assignments { pluginId } }
            }
            """, new { input = new { probeId, pluginIds = new[] { "WIFI_SCAN_ACTION" } } });

        var triggerPayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: TriggerProbeActionInputTypeInput!) {
              triggerProbeAction(input: $input) {
                success
                execution { executionId probeId pluginId status }
              }
            }
            """, new { input = new { probeId, pluginId = "WIFI_SCAN_ACTION", triggeredBy = "admin" } });

        var execution = triggerPayload.GetProperty("data").GetProperty("triggerProbeAction").GetProperty("execution");
        var executionId = execution.GetProperty("executionId").GetString();
        Assert.Equal("QUEUED", execution.GetProperty("status").GetString());

        var pendingPayload = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              pendingProbeActions(probeId: $probeId, limit: 10) {
                executionId
                pluginId
                status
              }
            }
            """, new { probeId });

        var pending = pendingPayload.GetProperty("data").GetProperty("pendingProbeActions").EnumerateArray().ToArray();
        Assert.Single(pending);
        Assert.Equal(executionId, pending[0].GetProperty("executionId").GetString());
        Assert.Equal("DELIVERED", pending[0].GetProperty("status").GetString());

        await IntegrationTestClient.UpdateActionStatusAsync(probe, probeId, executionId!, "RUNNING");
        var completedPayload = await IntegrationTestClient.UpdateActionStatusAsync(probe, probeId, executionId!, "SUCCEEDED");
        Assert.Equal("SUCCEEDED", completedPayload.GetProperty("data").GetProperty("updateProbeActionStatus").GetProperty("execution").GetProperty("status").GetString());

        var historyPayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            query($probeId: String!) {
              probeActionExecutions(probeId: $probeId, limit: 10) {
                executionId
                status
                pluginId
              }
            }
            """, new { probeId });

        var history = historyPayload.GetProperty("data").GetProperty("probeActionExecutions").EnumerateArray().ToArray();
        Assert.Contains(history, item =>
            item.GetProperty("executionId").GetString() == executionId
            && item.GetProperty("status").GetString() == "SUCCEEDED");
    }
}
