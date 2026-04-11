namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
public class ProbeTestConfigurationRepositoryAdapter : IProbeTestConfigurationRepository
{
    private readonly CentralServerDbContext _context;

    public ProbeTestConfigurationRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProbeTestConfiguration>> GetByProbeIdAsync(
        ProbeId probeId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ProbeTestConfigurations
            .Where(pc => pc.ProbeId == probeId.Value)
            .Include(pc => pc.TestTypeEntity)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<ProbeTestConfiguration>> GetEnabledByProbeIdAsync(
        ProbeId probeId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ProbeTestConfigurations
            .Where(pc => pc.ProbeId == probeId.Value && pc.Enabled)
            .Include(pc => pc.TestTypeEntity)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<ProbeTestConfiguration?> GetAsync(
        ProbeId probeId,
        string testTypeName,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProbeTestConfigurations
            .Where(pc => pc.ProbeId == probeId.Value && pc.TestType == testTypeName)
            .Include(pc => pc.TestTypeEntity)
            .FirstOrDefaultAsync(cancellationToken);

        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task UpdateAsync(ProbeTestConfiguration config, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProbeTestConfigurations
            .FirstOrDefaultAsync(pc => pc.ProbeId == config.ProbeId.Value && pc.TestType == config.TestType.Name, cancellationToken);

        if (entity == null)
        {
            entity = new ProbeTestConfigEntity
            {
                ProbeId = config.ProbeId.Value,
                TestType = config.TestType.Name
            };
            _context.ProbeTestConfigurations.Add(entity);
        }

        entity.IntervalSeconds = config.IntervalSeconds;
        entity.Enabled = config.Enabled;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ProbeId probeId, string testTypeName, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProbeTestConfigurations
            .FirstOrDefaultAsync(pc => pc.ProbeId == probeId.Value && pc.TestType == testTypeName, cancellationToken)
            ?? throw new InvalidOperationException($"Configuration not found for probe {probeId} and test type {testTypeName}");

        _context.ProbeTestConfigurations.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ProbeTestConfiguration MapToDomain(ProbeTestConfigEntity entity)
    {
        var testType = new TestType(entity.TestType, entity.TestTypeEntity?.Description ?? "Unknown test type");
        return new ProbeTestConfiguration(
            new ProbeId(entity.ProbeId),
            testType,
            entity.IntervalSeconds,
            entity.Enabled
        );
    }
}
