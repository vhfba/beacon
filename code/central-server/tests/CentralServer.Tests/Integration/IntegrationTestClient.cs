namespace CentralServer.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

internal static class IntegrationTestClient
{
    public static async Task<JsonElement> PostGraphQLAsync(HttpClient client, string query, object? variables = null)
    {
        var response = await client.PostAsJsonAsync("/graphql", new { query, variables });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (payload.TryGetProperty("errors", out var errors))
        {
            throw new Xunit.Sdk.XunitException($"GraphQL errors: {errors}");
        }

        return payload;
    }

    public static object HeartbeatInput(string probeId, string ipAddress)
    {
        return new
        {
            probeId,
            name = $"Name-{probeId}",
            location = "Test-Site",
            ipAddress,
            ssid = "BEACON",
            agentVersion = "1.0.0"
        };
    }

    public static async Task ReportCoverageMetricsAsync(
        HttpClient probe,
        string probeId,
        string site,
        double rssi,
        double snr,
        double linkQuality,
        double latency,
        double loss)
    {
        await PostGraphQLAsync(probe, """
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
                probeId,
                samples = new[]
                {
                    Metric("beacon_wifi_rssi_dbm", rssi, probeId, site),
                    Metric("beacon_wifi_snr_db", snr, probeId, site),
                    Metric("beacon_wifi_link_quality_percent", linkQuality, probeId, site),
                    Metric("beacon_ping_latency_ms", latency, probeId, site),
                    Metric("beacon_ping_packet_loss_percent", loss, probeId, site)
                }
            }
        });
    }

    public static Task<JsonElement> UpdateActionStatusAsync(HttpClient probe, string probeId, string executionId, string status)
    {
        return PostGraphQLAsync(probe, """
            mutation($input: UpdateProbeActionStatusInputTypeInput!) {
              updateProbeActionStatus(input: $input) {
                success
                execution { executionId status }
              }
            }
            """, new { input = new { probeId, executionId, status } });
    }

    private static object Metric(string name, double value, string probeId, string site)
    {
        return new
        {
            name,
            kind = "gauge",
            value,
            labels = new[]
            {
                new { key = "probe_id", value = probeId },
                new { key = "site", value = site },
                new { key = "test_type", value = "PING" }
            }
        };
    }
}
