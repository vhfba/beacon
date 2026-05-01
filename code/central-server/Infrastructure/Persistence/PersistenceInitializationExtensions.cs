namespace CentralServer.Infrastructure.DependencyInjection;

using CentralServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class PersistenceInitializationExtensions
{
    public static async Task InitializePersistenceAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CentralServerDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
