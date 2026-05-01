namespace CentralServer.Application.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Application.PluginDistribution;
using CentralServer.Domain.Models;

public static class ApplicationDtoMappings
{
    public static ProbeDTO ToDto(this Probe probe)
    {
        return new ProbeDTO
        {
            Id = probe.Id.Value,
            Name = probe.Name,
            Location = probe.Location,
            IpAddress = probe.IpAddress,
            Ssid = probe.Ssid,
            AgentVersion = probe.AgentVersion,
            Status = probe.Status.ToString(),
            CreatedAt = probe.CreatedAt,
            LastHeartbeat = probe.LastHeartbeat,
            LastConfigFetch = probe.LastConfigFetch,
            LastMetricsPush = probe.LastMetricsPush,
            LastSeenAt = probe.LastSeenAt
        };
    }

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

    public static ProbeTestConfigurationDTO ToDto(this ProbeTestConfiguration config)
    {
        return new ProbeTestConfigurationDTO
        {
            ProbeId = config.ProbeId.Value,
            TestType = config.TestType.Name,
            IntervalSeconds = config.IntervalSeconds,
            Enabled = config.Enabled
        };
    }

    public static ProbeActionExecutionDTO ToDto(this ProbeActionExecution execution)
    {
        return new ProbeActionExecutionDTO
        {
            ExecutionId = execution.ExecutionId,
            ProbeId = execution.ProbeId.Value,
            PluginId = execution.PluginId,
            TriggeredBy = execution.TriggeredBy,
            Status = execution.Status,
            RequestedAtUtc = execution.RequestedAtUtc,
            DeliveredAtUtc = execution.DeliveredAtUtc,
            StartedAtUtc = execution.StartedAtUtc,
            CompletedAtUtc = execution.CompletedAtUtc,
            ErrorMessage = execution.ErrorMessage
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

    public static ProbeRuntimeDTO ToRuntimeDto(this Probe probe, IReadOnlyList<ProbeTestConfiguration> enabledTests)
    {
        return new ProbeRuntimeDTO
        {
            ProbeId = probe.Id.Value,
            Status = probe.Status.ToString().ToUpperInvariant(),
            CanEmitMetrics = probe.Status == ProbeStatus.Active,
            Site = probe.Location,
            IpAddress = probe.IpAddress,
            PolledAtUtc = DateTimeOffset.UtcNow,
            EnabledTests = enabledTests
                .Select(test => test.TestType.Name.ToUpperInvariant())
                .Distinct()
                .OrderBy(name => name)
                .ToList()
        };
    }

    public static ProbeConfigDTO ToConfigDto(
        string probeId,
        IReadOnlyList<ProbeTestConfiguration> configs,
        IReadOnlyList<ProbePluginAssignment> assignments,
        IReadOnlyList<Plugin> availablePlugins)
    {
        var assignedPluginIds = assignments
            .Select(a => a.PluginId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ProbeConfigDTO
        {
            ProbeId = probeId,
            EnabledTests = configs.Select(ToDto).ToList(),
            AvailablePlugins = availablePlugins
                .Where(p => assignedPluginIds.Contains(p.Id))
                .OrderBy(p => p.Name)
                .ThenByDescending(p => p.ReleasedAt)
                .Select(ToDto)
                .ToList()
        };
    }
}
