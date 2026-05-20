namespace CentralServer.Application.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;

public static partial class ApplicationDtoMappings
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
                .Select(test => test.PluginId.ToUpperInvariant())
                .Distinct()
                .OrderBy(name => name)
                .ToList()
        };
    }
}
