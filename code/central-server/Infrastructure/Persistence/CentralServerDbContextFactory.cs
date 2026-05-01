namespace CentralServer.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class CentralServerDbContextFactory : IDesignTimeDbContextFactory<CentralServerDbContext>
{
    public CentralServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CentralServerDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=beacon_central;Username=beacon;Password=beacon;";

        optionsBuilder.UseNpgsql(connectionString);
        return new CentralServerDbContext(optionsBuilder.Options);
    }
}
