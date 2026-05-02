namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using CentralServer.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

public class ProbeRepositoryAdapter : IProbeRepository
{
    private readonly CentralServerDbContext _context;

    public ProbeRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public Task<Probe> RegisterAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        var entity = probe.ToEntity();
        _context.Probes.Add(entity);
        return Task.FromResult(entity.ToDomain());
    }

    public async Task<Probe?> GetByIdAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Probes.FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);
        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Probe>> GetAllAsync(ProbeStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Probes.AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value.ToString());

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<Probe?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Probes.FirstOrDefaultAsync(p => p.IpAddress == ipAddress, cancellationToken);
        return entity?.ToDomain();
    }

    public async Task UpdateAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(probe.Id, cancellationToken);
        probe.ApplyToEntity(entity);

        _context.Probes.Update(entity);
    }

    public async Task DeleteAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(id, cancellationToken);
        _context.Probes.Remove(entity);
    }

    private async Task<ProbeEntity> GetRequiredEntityAsync(
        ProbeId id,
        CancellationToken cancellationToken)
    {
        return await _context.Probes.FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Probe {id.Value} not found");
    }
}
