using CentralServer.Application.DTOs;

namespace CentralServer.Application.Abstractions;

public interface IGrafanaDashboardClient
{
    Task<IReadOnlyList<GrafanaDashboardSummary>> ListDashboardsAsync(CancellationToken cancellationToken);

    Task<GrafanaSyncResult> UpsertPluginDashboardAsync(
        string pluginId,
        string? title,
        System.Text.Json.Nodes.JsonObject dashboard,
        CancellationToken cancellationToken);

    string BuildEmbedUrl(string dashboardUid, string site);
}
