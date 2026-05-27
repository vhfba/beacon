namespace CentralServer.Tests.Integration.BlackBox;

using CentralServer.Domain.Models;
using CentralServer.Tests.Integration;

public class ControlCommandFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public ControlCommandFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WifiScanCommand_CanBeQueuedClaimedUpdatedAndReviewed()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        var probeId = "probe-control-flow";
        await _factory.SeedProbeAsync(probeId, "10.32.0.1", ProbeStatus.Active);

        var requestPayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: RequestWifiScanInputTypeInput!) {
              requestWifiScan(input: $input) {
                success
                command { commandId probeId type status requestedBy }
              }
            }
            """, new { input = new { probeId, requestedBy = "admin" } });

        var command = requestPayload.GetProperty("data").GetProperty("requestWifiScan").GetProperty("command");
        var commandId = command.GetProperty("commandId").GetString();
        Assert.Equal("SCAN_WIFI_NETWORKS", command.GetProperty("type").GetString());
        Assert.Equal("QUEUED", command.GetProperty("status").GetString());

        var pendingPayload = await IntegrationTestClient.PostGraphQLAsync(probe, """
            query($probeId: String!) {
              pendingProbeControlCommands(probeId: $probeId, limit: 10) {
                commandId
                type
                status
              }
            }
            """, new { probeId });
        var pending = pendingPayload.GetProperty("data").GetProperty("pendingProbeControlCommands").EnumerateArray().ToArray();
        Assert.Single(pending);
        Assert.Equal(commandId, pending[0].GetProperty("commandId").GetString());
        Assert.Equal("DELIVERED", pending[0].GetProperty("status").GetString());

        await UpdateCommandStatusAsync(probe, probeId, commandId!, "RUNNING", null);
        var completedPayload = await UpdateCommandStatusAsync(probe, probeId, commandId!, "SUCCEEDED", """{"networks":3}""");
        Assert.Equal("SUCCEEDED", completedPayload.GetProperty("data").GetProperty("updateProbeControlCommandStatus").GetProperty("command").GetProperty("status").GetString());

        var historyPayload = await IntegrationTestClient.PostGraphQLAsync(admin, """
            query($probeId: String!) {
              probeControlCommands(probeId: $probeId, limit: 10) {
                commandId
                status
                resultJson
              }
            }
            """, new { probeId });
        var history = historyPayload.GetProperty("data").GetProperty("probeControlCommands").EnumerateArray().ToArray();
        Assert.Contains(history, item =>
            item.GetProperty("commandId").GetString() == commandId
            && item.GetProperty("status").GetString() == "SUCCEEDED"
            && item.GetProperty("resultJson").GetString() == """{"networks":3}""");
    }

    private static Task<System.Text.Json.JsonElement> UpdateCommandStatusAsync(
        HttpClient probe,
        string probeId,
        string commandId,
        string status,
        string? resultJson)
    {
        return IntegrationTestClient.PostGraphQLAsync(probe, """
            mutation($input: UpdateProbeControlCommandStatusInputTypeInput!) {
              updateProbeControlCommandStatus(input: $input) {
                success
                command { commandId status resultJson }
              }
            }
            """, new { input = new { probeId, commandId, status, resultJson } });
    }
}
