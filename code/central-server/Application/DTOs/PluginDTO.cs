namespace CentralServer.Application.DTOs;

using CentralServer.Domain.Models;

public record PluginDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BundleDownloadUrl { get; init; }
    public string? DashboardJson { get; init; }
    public DateTime ReleasedAt { get; init; }
    public bool Available { get; init; }
    public PluginExecutionMode ExecutionMode { get; init; }
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
            BundleDownloadUrl = plugin.BundleDownloadUrl,
            DashboardJson = plugin.DashboardJson,
            ReleasedAt = plugin.ReleasedAt,
            Available = plugin.Available,
            ExecutionMode = plugin.ExecutionMode,
            BundleUrl = $"/plugins/{plugin.Id}/{plugin.Version}/bundle"
        };
    }
}
