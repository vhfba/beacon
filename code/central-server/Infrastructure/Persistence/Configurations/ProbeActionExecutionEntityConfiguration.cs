namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProbeActionExecutionEntityConfiguration : IEntityTypeConfiguration<ProbeActionExecutionEntity>
{
    public void Configure(EntityTypeBuilder<ProbeActionExecutionEntity> builder)
    {
        builder.HasQueryFilter(e => !e.Plugin!.IsDeleted);

        builder
            .HasIndex(e => e.ProbeId)
            .HasDatabaseName("idx_probe_action_probe_id");

        builder
            .HasIndex(e => e.Status)
            .HasDatabaseName("idx_probe_action_status");

        builder
            .HasIndex(e => new { e.ProbeId, e.Status, e.RequestedAtUtc })
            .HasDatabaseName("idx_probe_action_probe_status_requested")
            .IsDescending(false, false, true);

        builder
            .HasOne(e => e.Probe)
            .WithMany()
            .HasForeignKey(e => e.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Plugin)
            .WithMany()
            .HasForeignKey(e => e.PluginId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(e => e.Status)
            .HasConversion<int>();
    }
}
