namespace CentralServer.Application.DTOs;

using CentralServer.Application.PluginDistribution;
using CentralServer.Domain.Models;

public record PluginDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime ReleasedAt { get; init; }
    public bool Available { get; init; }
    public string BundleUrl { get; init; } = string.Empty;

    public static PluginDTO FromDomain(Plugin plugin)
    {
        return new PluginDTO
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Version = plugin.Version,
            Checksum = plugin.Checksum,
            Description = plugin.Description,
            ReleasedAt = plugin.ReleasedAt,
            Available = plugin.Available,
            BundleUrl = PluginBundleConventions.BuildBundleUrl(plugin.Id, plugin.Version)
        };
    }
}
