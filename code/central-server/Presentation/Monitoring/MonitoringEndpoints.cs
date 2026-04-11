namespace CentralServer.Presentation.Monitoring;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/monitoring/prometheus/service-discovery", async (
            HttpRequest request,
            IProbeRepository probeRepository,
            IOptionsSnapshot<MonitoringOptions> monitoringOptions,
            CancellationToken cancellationToken) =>
        {
            var options = monitoringOptions.Value.Prometheus;
            if (string.IsNullOrWhiteSpace(options.ServiceDiscoveryToken))
            {
                return Results.Problem(
                    detail: "Prometheus service discovery token is not configured.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var providedToken = request.Query["token"].ToString();
            if (string.IsNullOrWhiteSpace(providedToken) && request.Headers.TryGetValue("X-Service-Discovery-Token", out var tokenHeader))
            {
                providedToken = tokenHeader.ToString();
            }

            if (!FixedTimeEquals(providedToken, options.ServiceDiscoveryToken))
            {
                return Results.Unauthorized();
            }

            var metricsPath = string.IsNullOrWhiteSpace(options.DefaultMetricsPath)
                ? "/metrics"
                : options.DefaultMetricsPath.Trim();
            if (!metricsPath.StartsWith('/'))
            {
                metricsPath = "/" + metricsPath;
            }

            var probes = await probeRepository.GetAllAsync(cancellationToken: cancellationToken);
            var targetGroups = probes
                .Where(probe => probe.Status == ProbeStatus.Active)
                .Where(probe => !string.IsNullOrWhiteSpace(probe.IpAddress))
                .Select(probe => new
                {
                    targets = new[] { BuildPrometheusTarget(probe.IpAddress, options.DefaultProbeMetricsPort) },
                    labels = new Dictionary<string, string>
                    {
                        ["probe_id"] = probe.Id.Value,
                        ["site"] = probe.Location,
                        ["status"] = probe.Status.ToString().ToLowerInvariant(),
                        ["__metrics_path__"] = metricsPath
                    }
                })
                .ToList();

            return Results.Json(targetGroups);
        })
            .WithName("PrometheusServiceDiscovery");

        endpoints.MapGet("/monitoring/thresholds/{site}", (
            string site,
            ThresholdProfileStore thresholdStore) =>
        {
            var normalizedSite = ThresholdProfileStore.NormalizeSite(site);
            return Results.Ok(new
            {
                site = normalizedSite,
                profile = thresholdStore.Get(normalizedSite)
            });
        })
            .WithName("GetMonitoringThresholdProfile")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        endpoints.MapPut("/monitoring/thresholds/{site}", async (
            string site,
            WifiThresholdProfile profile,
            ThresholdProfileStore thresholdStore,
            IOptionsSnapshot<MonitoringOptions> monitoringOptions,
            GrafanaDashboardSyncService grafanaSyncService,
            CancellationToken cancellationToken) =>
        {
            var normalizedSite = ThresholdProfileStore.NormalizeSite(site);
            var storedProfile = thresholdStore.Set(normalizedSite, profile);
            var syncResult = await grafanaSyncService.EnsureSiteDashboardAsync(
                normalizedSite,
                storedProfile,
                cancellationToken);

            var dashboardUid = syncResult.Applied
                ? syncResult.DashboardUid
                : monitoringOptions.Value.Grafana.DashboardBaseUid;

            return Results.Ok(new
            {
                site = normalizedSite,
                profile = storedProfile,
                grafana = new
                {
                    applied = syncResult.Applied,
                    message = syncResult.Message,
                    dashboardUid,
                    embedUrl = grafanaSyncService.BuildEmbedUrl(dashboardUid, normalizedSite)
                }
            });
        })
            .WithName("SetMonitoringThresholdProfile")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        endpoints.MapPost("/monitoring/grafana/embed-session", async (
            GrafanaEmbedSessionRequest request,
            ThresholdProfileStore thresholdStore,
            IOptionsSnapshot<MonitoringOptions> monitoringOptions,
            GrafanaDashboardSyncService grafanaSyncService,
            CancellationToken cancellationToken) =>
        {
            var normalizedSite = ThresholdProfileStore.NormalizeSite(request.Site);
            var profile = thresholdStore.Get(normalizedSite);
            var syncResult = await grafanaSyncService.EnsureSiteDashboardAsync(
                normalizedSite,
                profile,
                cancellationToken);

            var dashboardUid = syncResult.Applied
                ? syncResult.DashboardUid
                : monitoringOptions.Value.Grafana.DashboardBaseUid;

            return Results.Ok(new
            {
                site = normalizedSite,
                dashboardUid,
                embedUrl = grafanaSyncService.BuildEmbedUrl(dashboardUid, normalizedSite),
                grafanaSyncApplied = syncResult.Applied,
                grafanaSyncMessage = syncResult.Message
            });
        })
            .WithName("CreateGrafanaEmbedSession")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return endpoints;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
        var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string BuildPrometheusTarget(string hostOrAddress, int defaultPort)
    {
        var value = (hostOrAddress ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"localhost:{defaultPort}";
        }

        // Handle bracketed IPv6 forms such as [2001:db8::1] or [2001:db8::1]:9464.
        if (value.StartsWith('['))
        {
            var closeBracket = value.IndexOf(']');
            if (closeBracket > 0)
            {
                var hostPart = value[..(closeBracket + 1)];
                if (closeBracket + 1 < value.Length && value[closeBracket + 1] == ':')
                {
                    var portSegment = value[(closeBracket + 2)..];
                    if (int.TryParse(portSegment, out var parsedPort) && parsedPort is > 0 and <= 65535)
                    {
                        return value;
                    }
                }

                return $"{hostPart}:{defaultPort}";
            }
        }

        var firstColon = value.IndexOf(':');
        var lastColon = value.LastIndexOf(':');

        // host:port form (single colon) should be respected as-is when port is valid.
        if (firstColon == lastColon && firstColon > 0 && lastColon < value.Length - 1)
        {
            var portSegment = value[(lastColon + 1)..];
            if (int.TryParse(portSegment, out var parsedPort) && parsedPort is > 0 and <= 65535)
            {
                return value;
            }
        }

        // Bare IPv6 addresses need brackets for Prometheus target format.
        if (value.Contains(':'))
        {
            return $"[{value}]:{defaultPort}";
        }

        return $"{value}:{defaultPort}";
    }
}

public sealed record GrafanaEmbedSessionRequest(string? Site);
