namespace CentralServer.Application.DTOs;

public sealed record DashboardAutomationSummary(
    int GrafanaApplied,
    int GrafanaSkippedOrFailed,
    string Mode,
    string? DashboardUid,
    string Message);
