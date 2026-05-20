namespace CentralServer.Tests.Unit.Services;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using CentralServer.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class GrafanaDashboardSyncServiceTests
{
    [Fact]
    public async Task UpsertPluginDashboardAsync_WithBasicCredentials_UsesBasicAuth()
    {
        AuthenticationHeaderValue? authorization = null;
        var service = CreateService(
            new GrafanaMonitoringOptions
            {
                ApiBaseUrl = "http://grafana",
                ApiUser = "admin",
                ApiPassword = "secret"
            },
            request =>
            {
                authorization = request.Headers.Authorization;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var result = await service.UpsertPluginDashboardAsync(
            "wifi-scan",
            "Wi-Fi Scan",
            new JsonObject { ["panels"] = new JsonArray() },
            CancellationToken.None);

        Assert.True(result.Applied);
        Assert.Equal("Basic", authorization?.Scheme);
    }

    [Fact]
    public async Task ListDashboardsAsync_ReturnsGrafanaDashboards()
    {
        var service = CreateService(
            new GrafanaMonitoringOptions
            {
                ApiBaseUrl = "http://grafana",
                ApiUser = "admin",
                ApiPassword = "secret"
            },
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {"uid":"beacon-probe-health","title":"BEACON Probe Health","url":"/d/beacon-probe-health/beacon-probe-health"},
                      {"uid":"beacon-plugin-ping","title":"BEACON Plugin - Ping Quality","url":"/d/beacon-plugin-ping/beacon-plugin-ping-quality"}
                    ]
                    """)
            });

        var dashboards = await service.ListDashboardsAsync(CancellationToken.None);

        Assert.Equal(2, dashboards.Count);
        Assert.Equal("beacon-plugin-ping", dashboards[0].Uid);
    }

    [Fact]
    public async Task UpsertPluginDashboardAsync_WhenGrafanaRejectsRequest_ReturnsResponseBody()
    {
        var service = CreateService(
            new GrafanaMonitoringOptions
            {
                ApiBaseUrl = "http://grafana",
                ApiToken = "token"
            },
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"message":"invalid dashboard"}""")
            });

        var result = await service.UpsertPluginDashboardAsync(
            "wifi-scan",
            "Wi-Fi Scan",
            new JsonObject { ["panels"] = new JsonArray() },
            CancellationToken.None);

        Assert.False(result.Applied);
        Assert.Contains("status 400", result.Message, StringComparison.Ordinal);
        Assert.Contains("invalid dashboard", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpsertPluginDashboardAsync_WithoutCredentials_SkipsSync()
    {
        var requested = false;
        var service = CreateService(
            new GrafanaMonitoringOptions { ApiBaseUrl = "http://grafana" },
            _ =>
            {
                requested = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var result = await service.UpsertPluginDashboardAsync(
            "wifi-scan",
            "Wi-Fi Scan",
            new JsonObject { ["panels"] = new JsonArray() },
            CancellationToken.None);

        Assert.False(result.Applied);
        Assert.False(requested);
        Assert.Contains("configure an API token or API user/password", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeletePluginDashboardAsync_UsesGrafanaDeleteByUid()
    {
        HttpRequestMessage? capturedRequest = null;
        var service = CreateService(
            new GrafanaMonitoringOptions
            {
                ApiBaseUrl = "http://grafana",
                ApiToken = "token"
            },
            request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var result = await service.DeletePluginDashboardAsync(
            "wifi-scan",
            CancellationToken.None);

        Assert.True(result.Applied);
        Assert.Equal(HttpMethod.Delete, capturedRequest?.Method);
        Assert.Equal("http://grafana/api/dashboards/uid/beacon-plugin-wifi-scan", capturedRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task DeletePluginDashboardAsync_WhenDashboardIsMissing_IsSuccessful()
    {
        var service = CreateService(
            new GrafanaMonitoringOptions
            {
                ApiBaseUrl = "http://grafana",
                ApiToken = "token"
            },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await service.DeletePluginDashboardAsync(
            "wifi-scan",
            CancellationToken.None);

        Assert.True(result.Applied);
        Assert.Contains("already absent", result.Message, StringComparison.Ordinal);
    }

    private static GrafanaDashboardSyncService CreateService(
        GrafanaMonitoringOptions grafanaOptions,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var options = new MonitoringOptions { Grafana = grafanaOptions };
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));
        return new GrafanaDashboardSyncService(
            httpClient,
            new StubOptionsMonitor(options),
            NullLogger<GrafanaDashboardSyncService>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class StubOptionsMonitor : IOptionsMonitor<MonitoringOptions>
    {
        public StubOptionsMonitor(MonitoringOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public MonitoringOptions CurrentValue { get; }

        public MonitoringOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<MonitoringOptions, string?> listener) => null;
    }
}
