namespace CentralServer.Application.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Application.PluginDistribution;
using CentralServer.Domain.Models;

public static partial class ApplicationDtoMappings
{
    public static PluginDTO ToDto(this Plugin plugin)
    {
        return new PluginDTO
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
            ExecutionMode = plugin.ExecutionMode,
            BundleUrl = string.IsNullOrWhiteSpace(plugin.BundleDownloadUrl)
                ? PluginBundleConventions.BuildBundleUrl(plugin.Id, plugin.Version)
                : plugin.BundleDownloadUrl
        };
    }

    public static ProbePluginAssignmentDTO ToDto(this ProbePluginAssignment assignment, Plugin plugin)
    {
        return new ProbePluginAssignmentDTO
        {
            ProbeId = assignment.ProbeId.Value,
            PluginId = assignment.PluginId,
            PluginName = plugin.Name,
            PluginVersion = plugin.Version,
            PluginAvailable = plugin.Available,
            AssignedAt = assignment.AssignedAt
        };
    }
}
