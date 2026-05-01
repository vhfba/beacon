namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProbeTestConfigEntityConfiguration : IEntityTypeConfiguration<ProbeTestConfigEntity>
{
    public void Configure(EntityTypeBuilder<ProbeTestConfigEntity> builder)
    {
        builder.HasKey(pc => new { pc.ProbeId, pc.TestType });

        builder
            .HasIndex(pc => pc.ProbeId)
            .HasDatabaseName("idx_probe_config_probe_id");

        builder
            .HasIndex(pc => pc.Enabled)
            .HasDatabaseName("idx_probe_config_enabled");

        builder
            .HasIndex(pc => new { pc.ProbeId, pc.Enabled })
            .HasDatabaseName("idx_probe_config_probe_enabled");
    }
}
