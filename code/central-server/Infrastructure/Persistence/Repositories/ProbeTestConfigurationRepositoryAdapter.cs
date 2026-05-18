namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using CentralServer.Infrastructure.Persistence.Mappings;
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
            .ToListAsync(cancellationToken);

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<ProbeTestConfiguration>> GetEnabledByProbeIdAsync(
        ProbeId probeId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ProbeTestConfigurations
            .Where(pc => pc.ProbeId == probeId.Value && pc.Enabled)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<ProbeTestConfiguration?> GetAsync(
        ProbeId probeId,
        string testTypeName,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProbeTestConfigurations
            .Where(pc => pc.ProbeId == probeId.Value && pc.TestType == testTypeName)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomain();
    }

    public async Task UpdateAsync(ProbeTestConfiguration config, CancellationToken cancellationToken = default)
    {
        var entity = await FindEntityAsync(config.ProbeId, config.TestType.Name, cancellationToken);

        if (entity == null)
        {
            entity = CreateEntity(config.ProbeId, config.TestType.Name);
            _context.ProbeTestConfigurations.Add(entity);
        }

        config.ApplyToEntity(entity);
    }

    public async Task DeleteAsync(ProbeId probeId, string testTypeName, CancellationToken cancellationToken = default)
    {
        var entity = await FindEntityAsync(probeId, testTypeName, cancellationToken)
            ?? throw new InvalidOperationException($"Configuration not found for probe {probeId} and test type {testTypeName}");

        _context.ProbeTestConfigurations.Remove(entity);
    }

    private async Task<ProbeTestConfigEntity?> FindEntityAsync(
        ProbeId probeId,
        string testTypeName,
        CancellationToken cancellationToken)
    {
        return await _context.ProbeTestConfigurations
            .FirstOrDefaultAsync(pc => pc.ProbeId == probeId.Value && pc.TestType == testTypeName, cancellationToken);
    }

    private static ProbeTestConfigEntity CreateEntity(ProbeId probeId, string testTypeName)
    {
        return new ProbeTestConfigEntity
        {
            ProbeId = probeId.Value,
            TestType = testTypeName
        };
    }
}
