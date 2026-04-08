namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
public class TestTypeRepositoryAdapter : ITestTypeRepository
{
    private readonly CentralServerDbContext _context;

    public TestTypeRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public async Task<TestType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TestTypes.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
        return entity != null ? new TestType(entity.Name, entity.Description) : null;
    }

    public async Task<IReadOnlyList<TestType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.TestTypes.ToListAsync(cancellationToken);
        return entities.Select(e => new TestType(e.Name, e.Description)).ToList();
    }

    public async Task<TestType> CreateAsync(TestType testType, CancellationToken cancellationToken = default)
    {
        var entity = new TestTypeEntity
        {
            Name = testType.Name,
            Description = testType.Description
        };

        _context.TestTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return testType;
    }
}
