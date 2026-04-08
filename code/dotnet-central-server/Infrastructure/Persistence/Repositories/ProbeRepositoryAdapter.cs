namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
public class ProbeRepositoryAdapter : IProbeRepository
{
    private readonly CentralServerDbContext _context;

    public ProbeRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public async Task<Probe> RegisterAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        var entity = new ProbeEntity
        {
            Id = probe.Id.Value,
            Name = probe.Name,
            Location = probe.Location,
            IpAddress = probe.IpAddress,
            Status = probe.Status.ToString(),
            CreatedAt = probe.CreatedAt,
            Version = probe.Version
        };

        _context.Probes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDomain(entity);
    }

    public async Task<Probe?> GetByIdAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Probes.FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<IReadOnlyList<Probe>> GetAllAsync(ProbeStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Probes.AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value.ToString());

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<Probe?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Probes.FirstOrDefaultAsync(p => p.IpAddress == ipAddress, cancellationToken);
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task UpdateAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Probes.FirstOrDefaultAsync(p => p.Id == probe.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Probe {probe.Id.Value} not found");

        entity.Name = probe.Name;
        entity.Location = probe.Location;
        entity.IpAddress = probe.IpAddress;
        entity.Status = probe.Status.ToString();
        entity.LastHeartbeat = probe.LastHeartbeat;
        entity.LastConfigFetch = probe.LastConfigFetch;
        entity.Version = probe.Version;

        _context.Probes.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Probes.FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Probe {id.Value} not found");

        _context.Probes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Probe MapToDomain(ProbeEntity entity)
    {
        return Probe.Rehydrate(
            entity.Id,
            entity.Name,
            entity.Location,
            entity.IpAddress,
            entity.Status,
            entity.CreatedAt,
            entity.LastHeartbeat,
            entity.LastConfigFetch,
            entity.Version
        );
    }
}
