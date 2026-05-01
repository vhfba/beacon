namespace CentralServer.Application.DTOs;

public sealed record GrafanaSyncResult(bool Applied, string DashboardUid, string Message);
