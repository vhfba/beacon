namespace CentralServer.Infrastructure.Persistence.Mappings;

using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence.Entities;

public static class ProbeTestConfigurationEntityMappings
{
    public static ProbeTestConfiguration ToDomain(this ProbeTestConfigEntity entity)
    {
        return new ProbeTestConfiguration(
            new ProbeId(entity.ProbeId),
            entity.TestType,
            entity.IntervalSeconds,
            entity.Enabled);
    }

    public static void ApplyToEntity(this ProbeTestConfiguration config, ProbeTestConfigEntity entity)
    {
        entity.IntervalSeconds = config.IntervalSeconds;
        entity.Enabled = config.Enabled;
    }
}
