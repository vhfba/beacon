namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProbePluginAssignmentEntityConfiguration : IEntityTypeConfiguration<ProbePluginAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<ProbePluginAssignmentEntity> builder)
    {
        builder.HasQueryFilter(pa => !pa.Plugin!.IsDeleted);

        builder.HasKey(pa => new { pa.ProbeId, pa.PluginId });

        builder
            .HasIndex(pa => pa.ProbeId)
            .HasDatabaseName("idx_probe_plugin_probe_id");

        builder
            .HasIndex(pa => pa.PluginId)
            .HasDatabaseName("idx_probe_plugin_plugin_id");

        builder
            .HasOne(pa => pa.Probe)
            .WithMany(p => p.PluginAssignments)
            .HasForeignKey(pa => pa.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(pa => pa.Plugin)
            .WithMany(p => p.ProbeAssignments)
            .HasForeignKey(pa => pa.PluginId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
