namespace CentralServer.Infrastructure.Persistence.Mappings;

using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence.Entities;

public static class ProbeTestConfigurationEntityMappings
{
    public static ProbeTestConfiguration ToDomain(this ProbeTestConfigEntity entity)
    {
        var testType = new TestType(entity.TestType, $"Scheduled probe check {entity.TestType}");
        return new ProbeTestConfiguration(
            new ProbeId(entity.ProbeId),
            testType,
            entity.IntervalSeconds,
            entity.Enabled);
    }

    public static void ApplyToEntity(this ProbeTestConfiguration config, ProbeTestConfigEntity entity)
    {
        entity.IntervalSeconds = config.IntervalSeconds;
        entity.Enabled = config.Enabled;
    }
}
