using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Monitoring;
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

    public async Task<IReadOnlyList<GrafanaDashboardSummary>> ListDashboardsAsync(CancellationToken cancellationToken)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl) || !HasGrafanaApiCredentials(options))
        {
            return [];
        }

        var request = new HttpRequestMessage(HttpMethod.Get, GrafanaDashboardConventions.BuildDashboardSearchApiUrl(options.ApiBaseUrl));
        request.Headers.Authorization = BuildAuthorizationHeader(options);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Grafana dashboard search failed: {StatusCode} {Body}", response.StatusCode, errorBody);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var results = JsonSerializer.Deserialize<List<GrafanaSearchResult>>(json, JsonOptions) ?? [];
            return results
                .Where(item => !string.IsNullOrWhiteSpace(item.Uid) && !string.IsNullOrWhiteSpace(item.Title))
                .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .Select(item => new GrafanaDashboardSummary(item.Uid!, item.Title!, item.Url ?? string.Empty))
                .ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Grafana dashboard search returned invalid JSON");
            return [];
        }
    }

    public async Task<GrafanaSyncResult> UpsertPluginDashboardAsync(
        string pluginId,
        string? title,
        JsonObject dashboard,
        CancellationToken cancellationToken)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        var dashboardUid = GrafanaDashboardConventions.BuildPluginDashboardUid(pluginId);

        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl) || !HasGrafanaApiCredentials(options))
        {
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Grafana API settings are missing; configure an API token or API user/password before plugin dashboard sync.");
        }

        try
        {
            dashboard["uid"] = dashboardUid;
            dashboard["id"] = null;
            dashboard["title"] = string.IsNullOrWhiteSpace(title)
                ? $"BEACON Plugin - {pluginId}"
                : title;

            return await SaveDashboardAsync(
                options,
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

    public async Task<GrafanaSyncResult> DeletePluginDashboardAsync(
        string pluginId,
        CancellationToken cancellationToken)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        var dashboardUid = GrafanaDashboardConventions.BuildPluginDashboardUid(pluginId);

        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl) || !HasGrafanaApiCredentials(options))
        {
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Grafana API settings are missing; configure an API token or API user/password before plugin dashboard removal.");
        }

        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                GrafanaDashboardConventions.BuildDashboardDeleteApiUrl(options.ApiBaseUrl, dashboardUid));
            request.Headers.Authorization = BuildAuthorizationHeader(options);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            {
                return new GrafanaSyncResult(
                    Applied: true,
                    DashboardUid: dashboardUid,
                    Message: response.StatusCode == HttpStatusCode.NotFound
                        ? $"Grafana dashboard for plugin '{pluginId}' was already absent."
                        : $"Grafana dashboard removed for plugin '{pluginId}'.");
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Grafana dashboard delete failed for UID {DashboardUid}: {StatusCode} {Body}", dashboardUid, response.StatusCode, errorBody);

            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: BuildFailureMessage((int)response.StatusCode, errorBody));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting plugin dashboard for {PluginId}", pluginId);
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Unexpected error while deleting plugin dashboard.");
        }
    }

    public string BuildEmbedUrl(string dashboardUid, string site)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        return GrafanaDashboardConventions.BuildEmbedUrl(options.EmbedBaseUrl, dashboardUid, site);
    }

    private async Task<GrafanaSyncResult> SaveDashboardAsync(
        GrafanaMonitoringOptions options,
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

        var saveRequest = new HttpRequestMessage(HttpMethod.Post, GrafanaDashboardConventions.BuildDashboardApiUrl(options.ApiBaseUrl));
        saveRequest.Headers.Authorization = BuildAuthorizationHeader(options);
        saveRequest.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        var saveResponse = await _httpClient.SendAsync(saveRequest, cancellationToken);
        if (!saveResponse.IsSuccessStatusCode)
        {
            var errorBody = await saveResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Grafana sync failed for UID {DashboardUid}: {StatusCode} {Body}", dashboardUid, saveResponse.StatusCode, errorBody);

            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: BuildFailureMessage((int)saveResponse.StatusCode, errorBody));
        }

        return new GrafanaSyncResult(
            Applied: true,
            DashboardUid: dashboardUid,
            Message: successMessage);
    }

    private static bool HasGrafanaApiCredentials(GrafanaMonitoringOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.ApiToken)
            || (!string.IsNullOrWhiteSpace(options.ApiUser) && !string.IsNullOrWhiteSpace(options.ApiPassword));
    }

    private static AuthenticationHeaderValue BuildAuthorizationHeader(GrafanaMonitoringOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiToken))
        {
            return new AuthenticationHeaderValue("Bearer", options.ApiToken);
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ApiUser}:{options.ApiPassword}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private static string BuildFailureMessage(int statusCode, string errorBody)
    {
        var message = $"Grafana dashboard import failed with status {statusCode}.";
        if (string.IsNullOrWhiteSpace(errorBody))
        {
            return message;
        }

        var trimmed = errorBody.Trim();
        var detail = trimmed.Length > 300 ? trimmed[..300] : trimmed;
        return $"{message} Grafana response: {detail}";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record GrafanaSearchResult(string? Uid, string? Title, string? Url);
}
