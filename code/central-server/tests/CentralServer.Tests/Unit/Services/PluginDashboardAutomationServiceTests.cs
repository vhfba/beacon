namespace CentralServer.Tests.Unit.Services;

using System.Text.Json.Nodes;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Services;
using CentralServer.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;

public class PluginDashboardAutomationServiceTests
{
    [Fact]
    public void ValidateDashboardJson_WithThresholdProfile_Throws()
    {
        var service = new PluginDashboardAutomationService(
            new FakeGrafanaDashboardClient(),
            NullLogger<PluginDashboardAutomationService>.Instance);

        var ex = Assert.Throws<DomainException>(() => service.ValidateDashboardJson(
            """
            {
              "thresholdProfile": {
                "rssiYellowDbm": -75
              }
            }
            """));

        Assert.Contains("Grafana dashboard JSON", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyDashboardJsonAsync_WithGrafanaDashboard_UpsertsPluginDashboard()
    {
        var service = new PluginDashboardAutomationService(
            new FakeGrafanaDashboardClient(),
            NullLogger<PluginDashboardAutomationService>.Instance);

        var summary = await service.ApplyDashboardJsonAsync(
            "plugin-a",
            """
            {
              "dashboard": {
                "title": "Plugin Dashboard",
                "panels": []
              }
            }
            """,
            CancellationToken.None);

        Assert.Equal("plugin-dashboard", summary.Mode);
        Assert.Equal(1, summary.GrafanaApplied);
        Assert.Equal("beacon-plugin-plugin-a", summary.DashboardUid);
    }

    [Fact]
    public async Task ApplyDashboardJsonAsync_WhenGrafanaIsUnavailable_ReturnsSkippedSummary()
    {
        var grafanaClient = new FakeGrafanaDashboardClient
        {
            PluginResultFactory = pluginId => new GrafanaSyncResult(false, $"beacon-plugin-{pluginId}", "missing config")
        };

        var service = new PluginDashboardAutomationService(
            grafanaClient,
            NullLogger<PluginDashboardAutomationService>.Instance);

        var summary = await service.ApplyDashboardJsonAsync(
            "plugin-a",
            """
            {
              "dashboard": {
                "title": "Plugin Dashboard",
                "panels": []
              }
            }
            """,
            CancellationToken.None);

        Assert.Equal("plugin-dashboard", summary.Mode);
        Assert.Equal(0, summary.GrafanaApplied);
        Assert.Equal(1, summary.GrafanaSkippedOrFailed);
    }

    private sealed class FakeGrafanaDashboardClient : IGrafanaDashboardClient
    {
        public Func<string, GrafanaSyncResult>? PluginResultFactory { get; init; }

        public Task<IReadOnlyList<GrafanaDashboardSummary>> ListDashboardsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GrafanaDashboardSummary>>([]);
        }

        public Task<GrafanaSyncResult> UpsertPluginDashboardAsync(
            string pluginId,
            string? title,
            JsonObject dashboard,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PluginResultFactory?.Invoke(pluginId) ?? new GrafanaSyncResult(true, $"beacon-plugin-{pluginId}", title ?? "ok"));
        }

        public string BuildEmbedUrl(string dashboardUid, string site) => $"/d/{dashboardUid}?site={site}";
    }
}
