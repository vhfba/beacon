namespace CentralServer.Infrastructure.Persistence.Mappings;

using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence.Entities;

public static class ProbeEntityMappings
{
    public static ProbeEntity ToEntity(this Probe probe)
    {
        return new ProbeEntity
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
            LastSeenAt = probe.LastSeenAt,
            Version = probe.Version
        };
    }

    public static void ApplyToEntity(this Probe probe, ProbeEntity entity)
    {
        entity.Name = probe.Name;
        entity.Location = probe.Location;
        entity.IpAddress = probe.IpAddress;
        entity.Ssid = probe.Ssid;
        entity.AgentVersion = probe.AgentVersion;
        entity.Status = probe.Status.ToString();
        entity.LastHeartbeat = probe.LastHeartbeat;
        entity.LastConfigFetch = probe.LastConfigFetch;
        entity.LastMetricsPush = probe.LastMetricsPush;
        entity.LastSeenAt = probe.LastSeenAt;
        entity.Version = probe.Version;
    }

    public static Probe ToDomain(this ProbeEntity entity)
    {
        return Probe.Rehydrate(
            entity.Id,
            entity.Name,
            entity.Location,
            entity.IpAddress,
            entity.Ssid,
            entity.AgentVersion,
            entity.Status,
            entity.CreatedAt,
            entity.LastHeartbeat,
            entity.LastConfigFetch,
            entity.LastMetricsPush,
            entity.LastSeenAt,
            entity.Version);
    }
}
