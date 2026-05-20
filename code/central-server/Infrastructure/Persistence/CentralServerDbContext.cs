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
    public DbSet<ProbeTestConfigEntity> ProbeTestConfigurations { get; set; }
    public DbSet<PluginEntity> Plugins { get; set; }
    public DbSet<ProbePluginAssignmentEntity> ProbePluginAssignments { get; set; }
    public DbSet<ProbeActionExecutionEntity> ProbeActionExecutions { get; set; }
    public DbSet<ProbeControlCommandEntity> ProbeControlCommands { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CentralServerDbContext).Assembly);
    }
}
