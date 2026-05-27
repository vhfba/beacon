namespace CentralServer.Tests.Integration.Contract;

using System.Net;
using CentralServer.Domain.Models;
using CentralServer.Tests.Integration;

public class MetricsContractTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public MetricsContractTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReportProbeMetrics_IgnoresSamplesWithoutNamesAndExportsAcceptedSamples()
    {
        await _factory.SeedProbeAsync("probe-invalid-samples", "10.30.0.1", ProbeStatus.Active);
        var probe = _factory.CreateProbeClient();

        var payload = await IntegrationTestClient.PostGraphQLAsync(probe, """
            mutation($input: ReportProbeMetricsInputTypeInput!) {
              reportProbeMetrics(input: $input) {
                success
                acceptedSamples
              }
            }
            """, new
        {
            input = new
            {
                probeId = "probe-invalid-samples",
                samples = new object[]
                {
                    new
                    {
                        name = "   ",
                        kind = "gauge",
                        value = 1,
                        labels = Array.Empty<object>()
                    },
                    new
                    {
                        name = "beacon_valid_metric",
                        kind = "gauge",
                        value = 2,
                        labels = new[] { new { key = "probe_id", value = "probe-invalid-samples" } }
                    }
                }
            }
        });

        var result = payload.GetProperty("data").GetProperty("reportProbeMetrics");
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal(1, result.GetProperty("acceptedSamples").GetInt32());

        var response = await probe.GetAsync("/metrics");
        var metrics = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("beacon_valid_metric", metrics, StringComparison.Ordinal);
        Assert.DoesNotContain("   ", metrics, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MetricsEndpoint_SeparatesMultipleProbesAndEscapesLabels()
    {
        await _factory.SeedProbeAsync("probe-export-a", "10.30.0.2", ProbeStatus.Active);
        await _factory.SeedProbeAsync("probe-export-b", "10.30.0.3", ProbeStatus.Active);
        var probe = _factory.CreateProbeClient();

        await ReportCustomMetricAsync(probe, "probe-export-a", "Building \"A\"", "line\nbreak", "probe\\a");
        await ReportCustomMetricAsync(probe, "probe-export-b", "Building B", "8.8.8.8", "probe-b");

        var response = await probe.GetAsync("/metrics");
        var metrics = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("beacon_contract_http_metric{probe_id=\"probe\\\\a\",site=\"Building \\\"A\\\"\",target=\"line\\nbreak\"} 12", metrics, StringComparison.Ordinal);
        Assert.Contains("beacon_contract_http_metric{probe_id=\"probe-b\",site=\"Building B\",target=\"8.8.8.8\"} 12", metrics, StringComparison.Ordinal);
    }

    private static Task ReportCustomMetricAsync(HttpClient probe, string probeId, string site, string target, string probeLabel)
    {
        return IntegrationTestClient.PostGraphQLAsync(probe, """
            mutation($input: ReportProbeMetricsInputTypeInput!) {
              reportProbeMetrics(input: $input) { success acceptedSamples }
            }
            """, new
        {
            input = new
            {
                probeId,
                samples = new[]
                {
                    new
                    {
                        name = "beacon_contract_http_metric",
                        kind = "gauge",
                        value = 12,
                        labels = new[]
                        {
                            new { key = "probe_id", value = probeLabel },
                            new { key = "site", value = site },
                            new { key = "target", value = target }
                        }
                    }
                }
            }
        });
    }
}
