using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Presentation.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralServer.Infrastructure.Monitoring;

public sealed class GrafanaDashboardSyncService : IGrafanaDashboardClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<MonitoringOptions> _monitoringOptions;
    private readonly ILogger<GrafanaDashboardSyncService> _logger;

    public GrafanaDashboardSyncService(
        HttpClient httpClient,
        IOptionsMonitor<MonitoringOptions> monitoringOptions,
        ILogger<GrafanaDashboardSyncService> logger)
    {
        _httpClient = httpClient;
        _monitoringOptions = monitoringOptions;
        _logger = logger;
    }

    public async Task<GrafanaSyncResult> UpsertPluginDashboardAsync(
        string pluginId,
        string? title,
        JsonObject dashboard,
        CancellationToken cancellationToken)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        var dashboardUid = BuildPluginDashboardUid(pluginId);

        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl) || string.IsNullOrWhiteSpace(options.ApiToken))
        {
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Grafana API settings are missing; plugin dashboard sync skipped.");
        }

        try
        {
            dashboard["uid"] = dashboardUid;
            dashboard["id"] = null;
            dashboard["title"] = string.IsNullOrWhiteSpace(title)
                ? $"BEACON Plugin - {pluginId}"
                : title;

            return await SaveDashboardAsync(
                options.ApiBaseUrl,
                options.ApiToken,
                dashboardUid,
                dashboard,
                $"Dashboard updated from plugin '{pluginId}' registration",
                $"Grafana dashboard synchronized for plugin '{pluginId}'.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while importing plugin dashboard for {PluginId}", pluginId);
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Unexpected error while importing plugin dashboard.");
        }
    }

    public string BuildEmbedUrl(string dashboardUid, string site)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        var encodedSite = Uri.EscapeDataString(string.IsNullOrWhiteSpace(site) ? "default" : site.Trim());
        return $"{options.EmbedBaseUrl.TrimEnd('/')}/d/{Uri.EscapeDataString(dashboardUid)}?kiosk&theme=light&var-site={encodedSite}";
    }

    private async Task<GrafanaSyncResult> SaveDashboardAsync(
        string apiBaseUrl,
        string apiToken,
        string dashboardUid,
        JsonObject dashboard,
        string changeMessage,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["dashboard"] = dashboard,
            ["overwrite"] = true,
            ["message"] = changeMessage
        };

        var saveRequest = new HttpRequestMessage(HttpMethod.Post, CombineUrl(apiBaseUrl, "/api/dashboards/db"));
        saveRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        saveRequest.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        var saveResponse = await _httpClient.SendAsync(saveRequest, cancellationToken);
        if (!saveResponse.IsSuccessStatusCode)
        {
            var errorBody = await saveResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Grafana sync failed for UID {DashboardUid}: {StatusCode} {Body}", dashboardUid, saveResponse.StatusCode, errorBody);

            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: $"Grafana dashboard import failed with status {(int)saveResponse.StatusCode}.");
        }

        return new GrafanaSyncResult(
            Applied: true,
            DashboardUid: dashboardUid,
            Message: successMessage);
    }

    private static string BuildPluginDashboardUid(string pluginId)
    {
        var slug = new string((pluginId ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') ? ch : '-')
            .ToArray())
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "plugin";
        }

        var uid = $"beacon-plugin-{slug}";
        return uid.Length <= 40 ? uid : uid[..40];
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
