namespace CentralServer.Infrastructure.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public PrometheusMonitoringOptions Prometheus { get; init; } = new();

    public GrafanaMonitoringOptions Grafana { get; init; } = new();
}

public sealed class PrometheusMonitoringOptions
{
    public string MetricsEndpointPath { get; init; } = "/metrics";
}

public sealed class GrafanaMonitoringOptions
{
    public string EmbedBaseUrl { get; init; } = "http://localhost:3001";

    public string ApiBaseUrl { get; init; } = "http://localhost:3001";

    public string DashboardBaseUid { get; init; } = "beacon-probe-health";

    public string ApiToken { get; init; } = string.Empty;

    public string ApiUser { get; init; } = string.Empty;

    public string ApiPassword { get; init; } = string.Empty;
}
