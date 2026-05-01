namespace CentralServer.Application.Services;

using System.Text.Json;
using System.Text.Json.Nodes;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using Microsoft.Extensions.Logging;

public sealed class PluginDashboardAutomationService
{
    private readonly IGrafanaDashboardClient _grafanaDashboardClient;
    private readonly ILogger<PluginDashboardAutomationService> _logger;

    public PluginDashboardAutomationService(
        IGrafanaDashboardClient grafanaDashboardClient,
        ILogger<PluginDashboardAutomationService> logger)
    {
        _grafanaDashboardClient = grafanaDashboardClient;
        _logger = logger;
    }

    public void ValidateDashboardJson(string dashboardJson)
    {
        var root = ParseDashboardJson(dashboardJson);
        if (TryExtractDashboard(root) is not null)
        {
            return;
        }

        throw new DomainException("Dashboard JSON must be a Grafana dashboard JSON object.");
    }

    public async Task<DashboardAutomationSummary> ApplyDashboardJsonAsync(
        string pluginId,
        string dashboardJson,
        CancellationToken cancellationToken)
    {
        var root = ParseDashboardJson(dashboardJson);
        var dashboard = TryExtractDashboard(root)
            ?? throw new DomainException("Grafana dashboard JSON is missing the dashboard object or panels.");

        var title = dashboard["title"]?.GetValue<string>();
        var result = await _grafanaDashboardClient.UpsertPluginDashboardAsync(
            pluginId,
            title,
            dashboard,
            cancellationToken);

        if (!result.Applied)
        {
            _logger.LogInformation(
                "Plugin dashboard sync skipped or failed for plugin {PluginId}: {Message}",
                pluginId,
                result.Message);
        }

        return new DashboardAutomationSummary(
            GrafanaApplied: result.Applied ? 1 : 0,
            GrafanaSkippedOrFailed: result.Applied ? 0 : 1,
            Mode: "plugin-dashboard",
            DashboardUid: result.DashboardUid,
            Message: result.Message);
    }

    private static JsonNode ParseDashboardJson(string dashboardJson)
    {
        if (string.IsNullOrWhiteSpace(dashboardJson))
            throw new DomainException("Dashboard JSON cannot be empty");

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(dashboardJson);
        }
        catch (JsonException ex)
        {
            throw new DomainException("Dashboard JSON is not valid JSON", ex);
        }

        return root ?? throw new DomainException("Dashboard JSON is empty");
    }

    private static JsonObject? TryExtractDashboard(JsonNode root)
    {
        var dashboard = root["dashboard"] as JsonObject ?? root as JsonObject;
        var panels = dashboard?["panels"] as JsonArray;
        if (dashboard is null || panels is null)
        {
            return null;
        }

        return dashboard.DeepClone() as JsonObject;
    }
}
