namespace CentralServer.Application.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;

public static partial class ApplicationDtoMappings
{
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
