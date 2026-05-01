namespace CentralServer.Infrastructure.Persistence;

using CentralServer.Application.Abstractions;

public sealed class CentralServerUnitOfWork : IUnitOfWork
{
    private readonly CentralServerDbContext _dbContext;

    public CentralServerUnitOfWork(CentralServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
