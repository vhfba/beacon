namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PluginEntityConfiguration : IEntityTypeConfiguration<PluginEntity>
{
    public void Configure(EntityTypeBuilder<PluginEntity> builder)
    {
        builder
            .HasIndex(p => p.Name)
            .HasDatabaseName("idx_plugins_name");

        builder
            .HasIndex(p => new { p.Name, p.Version })
            .HasDatabaseName("idx_plugins_name_version")
            .IsUnique();

        builder
            .HasIndex(p => p.Available)
            .HasDatabaseName("idx_plugins_available");

        builder
            .HasIndex(p => p.ReleasedAt)
            .HasDatabaseName("idx_plugins_released_at")
            .IsDescending();

        builder
            .HasIndex(p => p.BundleDownloadUrl)
            .HasDatabaseName("idx_plugins_bundle_download_url");

        builder
            .Property(p => p.ExecutionMode)
            .HasConversion<int>();
    }
}
