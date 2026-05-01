namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProbeEntityConfiguration : IEntityTypeConfiguration<ProbeEntity>
{
    public void Configure(EntityTypeBuilder<ProbeEntity> builder)
    {
        builder
            .HasIndex(p => p.Status)
            .HasDatabaseName("idx_probes_status");

        builder
            .HasIndex(p => p.CreatedAt)
            .HasDatabaseName("idx_probes_created_at")
            .IsDescending();

        builder
            .HasIndex(p => p.LastHeartbeat)
            .HasDatabaseName("idx_probes_last_heartbeat")
            .IsDescending();

        builder
            .HasIndex(p => p.IpAddress)
            .IsUnique()
            .HasDatabaseName("unique_ip_probe");
    }
}
