namespace CentralServer.Infrastructure.Persistence;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
public class CentralServerDbContext : DbContext
{
    public CentralServerDbContext(DbContextOptions<CentralServerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProbeEntity> Probes { get; set; }
    public DbSet<TestTypeEntity> TestTypes { get; set; }
    public DbSet<ProbeTestConfigEntity> ProbeTestConfigurations { get; set; }
    public DbSet<PluginEntity> Plugins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ProbeEntity>()
            .HasIndex(p => p.Status)
            .HasDatabaseName("idx_probes_status");

        modelBuilder.Entity<ProbeEntity>()
            .HasIndex(p => p.CreatedAt)
            .HasDatabaseName("idx_probes_created_at")
            .IsDescending();

        modelBuilder.Entity<ProbeEntity>()
            .HasIndex(p => p.LastHeartbeat)
            .HasDatabaseName("idx_probes_last_heartbeat")
            .IsDescending();

        modelBuilder.Entity<ProbeEntity>()
            .HasIndex(p => p.IpAddress)
            .IsUnique()
            .HasDatabaseName("unique_ip_probe");
        modelBuilder.Entity<ProbeTestConfigEntity>()
            .HasKey(pc => new { pc.ProbeId, pc.TestType });

        modelBuilder.Entity<ProbeTestConfigEntity>()
            .HasIndex(pc => pc.ProbeId)
            .HasDatabaseName("idx_probe_config_probe_id");

        modelBuilder.Entity<ProbeTestConfigEntity>()
            .HasIndex(pc => pc.Enabled)
            .HasDatabaseName("idx_probe_config_enabled");

        modelBuilder.Entity<ProbeTestConfigEntity>()
            .HasIndex(pc => new { pc.ProbeId, pc.Enabled })
            .HasDatabaseName("idx_probe_config_probe_enabled");
        modelBuilder.Entity<PluginEntity>()
            .HasIndex(p => p.Name)
            .HasDatabaseName("idx_plugins_name");

        modelBuilder.Entity<PluginEntity>()
            .HasIndex(p => new { p.Name, p.Version })
            .HasDatabaseName("idx_plugins_name_version")
            .IsUnique();

        modelBuilder.Entity<PluginEntity>()
            .HasIndex(p => p.Available)
            .HasDatabaseName("idx_plugins_available");

        modelBuilder.Entity<PluginEntity>()
            .HasIndex(p => p.ReleasedAt)
            .HasDatabaseName("idx_plugins_released_at")
            .IsDescending();
        modelBuilder.Entity<TestTypeEntity>().HasData(
            new TestTypeEntity { Name = "RSSI", Description = "Receive Signal Strength Indicator measurement" },
            new TestTypeEntity { Name = "PING", Description = "ICMP echo request to measure latency" },
            new TestTypeEntity { Name = "HTTP", Description = "HTTP connectivity and response time test" },
            new TestTypeEntity { Name = "IPERF", Description = "Network throughput measurement" }
        );
    }
}
