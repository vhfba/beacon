namespace CentralServer.Presentation.Monitoring;

using CentralServer.Application.Abstractions;
using CentralServer.Application.UseCases;
using CentralServer.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/metrics", async (
            ExportPrometheusMetricsUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var payload = await useCase.ExecuteAsync(cancellationToken);
            return Results.Text(payload, "text/plain; version=0.0.4; charset=utf-8");
        })
            .WithName("PrometheusMetrics");

        endpoints.MapPost("/monitoring/grafana/embed-session", (
            GrafanaEmbedSessionRequest request,
            IOptionsSnapshot<MonitoringOptions> monitoringOptions,
            IGrafanaDashboardClient grafanaDashboardClient) =>
        {
            var site = string.IsNullOrWhiteSpace(request.Site) ? "default" : request.Site.Trim();
            var dashboardUid = monitoringOptions.Value.Grafana.DashboardBaseUid;

            return Results.Ok(new
            {
                site,
                dashboardUid,
                embedUrl = grafanaDashboardClient.BuildEmbedUrl(dashboardUid, site),
                grafanaSyncApplied = false,
                grafanaSyncMessage = "Site-specific Grafana threshold updates are disabled. Dashboard sync only happens during plugin registration."
            });
        })
            .WithName("CreateGrafanaEmbedSession")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return endpoints;
    }
}

public sealed record GrafanaEmbedSessionRequest(string? Site);
