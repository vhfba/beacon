namespace CentralServer.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CentralServer.Application.Abstractions;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class ProbeRuntimeGraphQLIntegrationTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public ProbeRuntimeGraphQLIntegrationTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecordProbeHeartbeat_WhenProbeUnknown_AutoRegistersAsRegistered()
    {
        var client = _factory.CreateProbeClient();
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($input: ProbeHeartbeatInputTypeInput!) {
                  recordProbeHeartbeat(input: $input) {
                    success
                    autoRegistered
                    probe { id status ipAddress }
                    runtime { status canEmitMetrics }
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    probeId = "probe-auto",
                    name = "Probe Auto",
                    location = "Test Site",
                    ipAddress = "10.9.0.1",
                    ssid = "Beacon",
                    agentVersion = "1.0.0"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var heartbeat = payload.GetProperty("data").GetProperty("recordProbeHeartbeat");
        Assert.True(heartbeat.GetProperty("success").GetBoolean());
        Assert.True(heartbeat.GetProperty("autoRegistered").GetBoolean());
        Assert.Equal("REGISTERED", heartbeat.GetProperty("probe").GetProperty("status").GetString());
        Assert.False(heartbeat.GetProperty("runtime").GetProperty("canEmitMetrics").GetBoolean());
    }

    [Fact]
    public async Task RegisterProbeMutation_IsNotExposedAnymore()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($input: RegisterProbeInputTypeInput!) {
                  registerProbe(input: $input) {
                    success
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    id = "probe-admin",
                    name = "Probe Admin",
                    location = "Admin",
                    ipAddress = "10.0.0.10"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = payload.GetProperty("errors").EnumerateArray().ToArray();

        Assert.NotEmpty(errors);
        Assert.Contains("registerProbe", errors[0].GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PendingActions_WhenProbeExists_ReturnsClaimedActionsViaGraphQL()
    {
        await _factory.SeedProbeAsync("probe-actions", "10.3.0.1", ProbeStatus.Active);
        await SeedActionAsync("probe-actions", "action-wifi", "admin");

        var client = _factory.CreateProbeClient();
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = """
                query($probeId: String!, $limit: Int) {
                  pendingProbeActions(probeId: $probeId, limit: $limit) {
                    probeId
                    pluginId
                    status
                  }
                }
                """,
            variables = new { probeId = "probe-actions", limit = 5 }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var actions = payload.GetProperty("data").GetProperty("pendingProbeActions").EnumerateArray().ToArray();
        Assert.Single(actions);
        Assert.Equal("probe-actions", actions[0].GetProperty("probeId").GetString());
        Assert.Equal("action-wifi", actions[0].GetProperty("pluginId").GetString());
        Assert.Equal("DELIVERED", actions[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReportProbeMetrics_WhenProbeExists_ExposesMetricsOnCentralEndpoint()
    {
        await _factory.SeedProbeAsync("probe-metrics", "10.3.0.5", ProbeStatus.Active);

        var client = _factory.CreateProbeClient();
        var mutationResponse = await client.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($input: ReportProbeMetricsInputTypeInput!) {
                  reportProbeMetrics(input: $input) {
                    success
                    acceptedSamples
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    probeId = "probe-metrics",
                    samples = new[]
                    {
                        new
                        {
                            name = "beacon_test_last_status",
                            kind = "gauge",
                            value = 1.0,
                            labels = new[]
                            {
                                new { key = "probe_id", value = "probe-metrics" },
                                new { key = "site", value = "Test-Site" },
                                new { key = "test_type", value = "PING" },
                                new { key = "target", value = "8.8.8.8" }
                            }
                        }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, mutationResponse.StatusCode);

        var metricsResponse = await client.GetAsync("/metrics");
        var body = await metricsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        Assert.Contains("beacon_test_last_status", body, StringComparison.Ordinal);
        Assert.Contains("probe_id=\"probe-metrics\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FleetCoverage_WhenMetricsExist_ReturnsCoverageSummary()
    {
        await _factory.SeedProbeAsync("probe-coverage", "10.3.0.6", ProbeStatus.Active);

        var probeClient = _factory.CreateProbeClient();
        var mutationResponse = await probeClient.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($input: ReportProbeMetricsInputTypeInput!) {
                  reportProbeMetrics(input: $input) {
                    success
                    acceptedSamples
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    probeId = "probe-coverage",
                    samples = new[]
                    {
                        Metric("beacon_wifi_rssi_dbm", -55),
                        Metric("beacon_wifi_snr_db", 35),
                        Metric("beacon_wifi_link_quality_percent", 94),
                        Metric("beacon_ping_latency_ms", 18),
                        Metric("beacon_ping_packet_loss_percent", 0)
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, mutationResponse.StatusCode);

        var adminClient = _factory.CreateAdminClient();
        var coverageResponse = await adminClient.PostAsJsonAsync("/graphql", new
        {
            query = """
                query {
                  fleetCoverage {
                    probeId
                    site
                    score
                    grade
                    rssiDbm
                    sampleCount
                  }
                }
                """
        });

        Assert.Equal(HttpStatusCode.OK, coverageResponse.StatusCode);
        var payload = await coverageResponse.Content.ReadFromJsonAsync<JsonElement>();
        var coverage = payload.GetProperty("data").GetProperty("fleetCoverage").EnumerateArray().ToArray();
        Assert.Contains(coverage, item =>
            item.GetProperty("probeId").GetString() == "probe-coverage"
            && item.GetProperty("grade").GetString() == "EXCELLENT"
            && item.GetProperty("score").GetInt32() == 100);
    }

    [Fact]
    public async Task ProbeConfig_WhenRequested_StillUpdatesLastConfigFetch()
    {
        await _factory.SeedProbeAsync("probe-config", "10.3.0.9", ProbeStatus.Active);

        var client = _factory.CreateProbeClient();
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = """
                query($probeId: String!) {
                  probeConfig(probeId: $probeId) {
                    probeId
                  }
                }
                """,
            variables = new { probeId = "probe-config" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProbeRepository>();
        var probe = await repository.GetByIdAsync(new ProbeId("probe-config"));

        Assert.NotNull(probe);
        Assert.NotNull(probe!.LastConfigFetch);
    }

    [Fact]
    public async Task DeletePlugin_WhenActionExecutionsExist_RemovesDependentRows()
    {
        const string probeId = "probe-delete-plugin";
        const string pluginId = "DELETE_ACTION";

        await _factory.SeedProbeAsync(probeId, "10.4.0.7", ProbeStatus.Active);
        using (var scope = _factory.Services.CreateScope())
        {
            var pluginRepository = scope.ServiceProvider.GetRequiredService<IPluginRepository>();
            var assignmentRepository = scope.ServiceProvider.GetRequiredService<IProbePluginAssignmentRepository>();
            var actionRepository = scope.ServiceProvider.GetRequiredService<IProbeActionExecutionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await pluginRepository.CreateAsync(new Plugin(pluginId, "Delete Action", "1.0.0", "sha-delete", executionMode: PluginExecutionMode.Action));
            await assignmentRepository.SetForProbeAsync(new ProbeId(probeId), [pluginId]);
            await actionRepository.CreateAsync(new ProbeActionExecution(new ProbeId(probeId), pluginId, "admin"));
            await unitOfWork.SaveChangesAsync();
        }

        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($pluginId: String!) {
                  deletePlugin(pluginId: $pluginId) {
                    success
                    message
                    pluginId
                  }
                }
                """,
            variables = new { pluginId }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var deleted = payload.GetProperty("data").GetProperty("deletePlugin");
        Assert.True(deleted.GetProperty("success").GetBoolean());
        Assert.Equal(pluginId, deleted.GetProperty("pluginId").GetString());

        using var verifyScope = _factory.Services.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<CentralServerDbContext>();
        Assert.False(await dbContext.Plugins.AnyAsync(p => p.Id == pluginId));
        Assert.False(await dbContext.ProbePluginAssignments.AnyAsync(a => a.PluginId == pluginId));
        Assert.False(await dbContext.ProbeActionExecutions.AnyAsync(a => a.PluginId == pluginId));
    }

    private async Task<string> SeedActionAsync(string probeId, string pluginId, string triggeredBy)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProbeActionExecutionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var execution = new ProbeActionExecution(new ProbeId(probeId), pluginId, triggeredBy);
        await repository.CreateAsync(execution);
        await unitOfWork.SaveChangesAsync();
        return execution.ExecutionId;
    }

    private static object Metric(string name, double value)
    {
        return new
        {
            name,
            kind = "gauge",
            value,
            labels = new[]
            {
                new { key = "probe_id", value = "probe-coverage" },
                new { key = "site", value = "Building A" }
            }
        };
    }
}
