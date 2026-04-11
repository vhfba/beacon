using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace CentralServer.Presentation.Monitoring;

public sealed class GrafanaDashboardSyncService
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

    public async Task<GrafanaSyncResult> EnsureSiteDashboardAsync(
        string site,
        WifiThresholdProfile profile,
        CancellationToken cancellationToken)
    {
        var normalizedSite = ThresholdProfileStore.NormalizeSite(site);
        var options = _monitoringOptions.CurrentValue.Grafana;
        var dashboardUid = BuildSiteDashboardUid(options.DashboardBaseUid, normalizedSite);

        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl) || string.IsNullOrWhiteSpace(options.ApiToken))
        {
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Grafana API settings are missing; threshold profile stored but dashboard sync skipped.");
        }

        try
        {
            var dashboard = await GetSiteOrBaseDashboardAsync(
                options.ApiBaseUrl,
                options.ApiToken,
                dashboardUid,
                options.DashboardBaseUid,
                cancellationToken);

            if (dashboard is null)
            {
                return new GrafanaSyncResult(
                    Applied: false,
                    DashboardUid: dashboardUid,
                    Message: "Grafana base dashboard was not found; cannot apply thresholds.");
            }

            dashboard["uid"] = dashboardUid;
            dashboard["title"] = $"BEACON Building Wi-Fi Quality - {normalizedSite}";
            ApplyThresholds(dashboard, profile);

            var payload = new JsonObject
            {
                ["dashboard"] = dashboard,
                ["overwrite"] = true,
                ["message"] = $"Thresholds updated for site '{normalizedSite}'"
            };

            var saveRequest = new HttpRequestMessage(HttpMethod.Post, CombineUrl(options.ApiBaseUrl, "/api/dashboards/db"));
            saveRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
            saveRequest.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            var saveResponse = await _httpClient.SendAsync(saveRequest, cancellationToken);
            if (!saveResponse.IsSuccessStatusCode)
            {
                var errorBody = await saveResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Grafana sync failed: {StatusCode} {Body}", saveResponse.StatusCode, errorBody);

                return new GrafanaSyncResult(
                    Applied: false,
                    DashboardUid: dashboardUid,
                    Message: $"Grafana sync failed with status {(int)saveResponse.StatusCode}.");
            }

            return new GrafanaSyncResult(
                Applied: true,
                DashboardUid: dashboardUid,
                Message: $"Grafana dashboard synchronized for site '{normalizedSite}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while syncing Grafana dashboard for site {Site}", normalizedSite);
            return new GrafanaSyncResult(
                Applied: false,
                DashboardUid: dashboardUid,
                Message: "Unexpected error while syncing Grafana dashboard.");
        }
    }

    public string BuildEmbedUrl(string dashboardUid, string site)
    {
        var options = _monitoringOptions.CurrentValue.Grafana;
        var encodedSite = Uri.EscapeDataString(ThresholdProfileStore.NormalizeSite(site));
        return $"{options.EmbedBaseUrl.TrimEnd('/')}/d/{Uri.EscapeDataString(dashboardUid)}?kiosk&theme=light&var-site={encodedSite}";
    }

    public static string BuildSiteDashboardUid(string baseUid, string site)
    {
        var safeBase = string.IsNullOrWhiteSpace(baseUid) ? "beacon-probe-health" : baseUid.Trim();
        var safeSite = SanitizeSlug(site);
        var candidate = $"{safeBase}-{safeSite}";
        return candidate.Length <= 40 ? candidate : candidate[..40];
    }

    private async Task<JsonObject?> GetSiteOrBaseDashboardAsync(
        string apiBaseUrl,
        string apiToken,
        string siteUid,
        string baseUid,
        CancellationToken cancellationToken)
    {
        var siteDashboard = await TryGetDashboardAsync(apiBaseUrl, apiToken, siteUid, cancellationToken);
        if (siteDashboard is not null)
        {
            return siteDashboard;
        }

        return await TryGetDashboardAsync(apiBaseUrl, apiToken, baseUid, cancellationToken);
    }

    private async Task<JsonObject?> TryGetDashboardAsync(
        string apiBaseUrl,
        string apiToken,
        string uid,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, CombineUrl(apiBaseUrl, $"/api/dashboards/uid/{uid}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Grafana dashboard lookup failed for UID {Uid}: {StatusCode} {Body}", uid, response.StatusCode, body);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(json) as JsonObject;
        var dashboard = root?["dashboard"] as JsonObject;
        return dashboard?.DeepClone() as JsonObject;
    }

    private static void ApplyThresholds(JsonObject dashboard, WifiThresholdProfile profile)
    {
        if (dashboard["panels"] is not JsonArray panels)
        {
            return;
        }

        foreach (var node in panels)
        {
            if (node is not JsonObject panel)
            {
                continue;
            }

            var title = panel["title"]?.GetValue<string>() ?? string.Empty;
            switch (title)
            {
                case "Avg RSSI (dBm)":
                    SetThresholdSteps(panel, "absolute", new[]
                    {
                        ("red", (double?)null),
                        ("yellow", profile.RssiYellowDbm),
                        ("green", profile.RssiGreenDbm)
                    });
                    break;
                case "Avg SNR (dB)":
                    SetThresholdSteps(panel, "absolute", new[]
                    {
                        ("red", (double?)null),
                        ("yellow", profile.SnrYellowDb),
                        ("green", profile.SnrGreenDb)
                    });
                    break;
                case "Link Quality (%)":
                    SetThresholdSteps(panel, "absolute", new[]
                    {
                        ("red", (double?)null),
                        ("yellow", profile.LinkQualityYellowPercent),
                        ("green", profile.LinkQualityGreenPercent)
                    });
                    break;
                case "Ping Loss (%)":
                    SetThresholdSteps(panel, "absolute", new[]
                    {
                        ("green", (double?)null),
                        ("yellow", profile.PingLossWarnPercent),
                        ("red", profile.PingLossCriticalPercent)
                    });
                    break;
                case "iPerf DL (Mbps)":
                    SetThresholdSteps(panel, "absolute", new[]
                    {
                        ("red", (double?)null),
                        ("yellow", profile.IperfDownloadWarnMbps),
                        ("green", profile.IperfDownloadGoodMbps)
                    });
                    break;
                case "iPerf UL (Mbps)":
                    SetThresholdSteps(panel, "absolute", new[]
                    {
                        ("red", (double?)null),
                        ("yellow", profile.IperfUploadWarnMbps),
                        ("green", profile.IperfUploadGoodMbps)
                    });
                    break;
            }
        }
    }

    private static void SetThresholdSteps(JsonObject panel, string mode, IEnumerable<(string Color, double? Value)> steps)
    {
        var fieldConfig = panel["fieldConfig"] as JsonObject ?? new JsonObject();
        panel["fieldConfig"] = fieldConfig;

        var defaults = fieldConfig["defaults"] as JsonObject ?? new JsonObject();
        fieldConfig["defaults"] = defaults;

        var thresholds = defaults["thresholds"] as JsonObject ?? new JsonObject();
        defaults["thresholds"] = thresholds;

        thresholds["mode"] = mode;

        var stepArray = new JsonArray();
        foreach (var (color, value) in steps)
        {
            var entry = new JsonObject
            {
                ["color"] = color,
                ["value"] = value is null ? null : JsonValue.Create(value.Value)
            };
            stepArray.Add(entry);
        }

        thresholds["steps"] = stepArray;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private static string SanitizeSlug(string value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "default" : value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(trimmed.Length);

        foreach (var ch in trimmed)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                builder.Append(ch);
                continue;
            }

            builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "default" : slug;
    }
}

public sealed record GrafanaSyncResult(bool Applied, string DashboardUid, string Message);
