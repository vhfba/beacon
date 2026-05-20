namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProbeControlCommandEntityConfiguration : IEntityTypeConfiguration<ProbeControlCommandEntity>
{
    public void Configure(EntityTypeBuilder<ProbeControlCommandEntity> builder)
    {
        builder
            .HasIndex(e => e.ProbeId)
            .HasDatabaseName("idx_probe_control_probe_id");

        builder
            .HasIndex(e => e.Status)
            .HasDatabaseName("idx_probe_control_status");

        builder
            .HasIndex(e => new { e.ProbeId, e.Status, e.RequestedAtUtc })
            .HasDatabaseName("idx_probe_control_probe_status_requested")
            .IsDescending(false, false, true);

        builder
            .HasOne(e => e.Probe)
            .WithMany(e => e.ControlCommands)
            .HasForeignKey(e => e.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Type).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();
    }
}
