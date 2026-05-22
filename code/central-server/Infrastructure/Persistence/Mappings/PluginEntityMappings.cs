namespace CentralServer.Infrastructure.Persistence.Mappings;

using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence.Entities;

public static class PluginEntityMappings
{
    public static PluginEntity ToEntity(this Plugin plugin)
    {
        return new PluginEntity
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Version = plugin.Version,
            Checksum = plugin.Checksum,
            Description = plugin.Description,
            BundleDownloadUrl = plugin.BundleDownloadUrl,
            DashboardJson = plugin.DashboardJson,
            ReleasedAt = plugin.ReleasedAt,
            Available = plugin.Available,
            ExecutionMode = plugin.ExecutionMode
        };
    }

    public static void ApplyToEntity(this Plugin plugin, PluginEntity entity)
    {
        entity.Name = plugin.Name;
        entity.Version = plugin.Version;
        entity.Checksum = plugin.Checksum;
        entity.Description = plugin.Description;
        entity.Available = plugin.Available;
        entity.BundleDownloadUrl = plugin.BundleDownloadUrl;
        entity.DashboardJson = plugin.DashboardJson;
        entity.ExecutionMode = plugin.ExecutionMode;
        entity.ReleasedAt = plugin.ReleasedAt;
    }

    public static Plugin ToDomain(this PluginEntity entity)
    {
        return Plugin.Rehydrate(
            entity.Id,
            entity.Name,
            entity.Version,
            entity.Checksum,
            entity.Description,
            entity.BundleDownloadUrl,
            entity.DashboardJson,
            entity.ReleasedAt,
            entity.Available,
            entity.ExecutionMode);
    }
}
