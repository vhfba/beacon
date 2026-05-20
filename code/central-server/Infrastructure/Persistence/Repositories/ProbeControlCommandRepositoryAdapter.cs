namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using CentralServer.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

public class ProbeControlCommandRepositoryAdapter : IProbeControlCommandRepository
{
    private readonly CentralServerDbContext _context;

    public ProbeControlCommandRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public Task<ProbeControlCommand> CreateAsync(ProbeControlCommand command, CancellationToken cancellationToken = default)
    {
        _context.ProbeControlCommands.Add(command.ToEntity());
        return Task.FromResult(command);
    }

    public async Task<ProbeControlCommand?> GetByIdAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProbeControlCommands
            .FirstOrDefaultAsync(e => e.CommandId == commandId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<ProbeControlCommand>> ClaimPendingForProbeAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);
        var entities = await _context.ProbeControlCommands
            .Where(e => e.ProbeId == probeId.Value && e.Status == ProbeControlCommandStatus.Queued)
            .OrderBy(e => e.RequestedAtUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.Status = ProbeControlCommandStatus.Delivered;
            entity.DeliveredAtUtc = now;
        }

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<ProbeControlCommand>> GetByProbeIdAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var entities = await _context.ProbeControlCommands
            .Where(e => e.ProbeId == probeId.Value)
            .OrderByDescending(e => e.RequestedAtUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task UpdateAsync(ProbeControlCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(command.CommandId, cancellationToken);
        command.ApplyToEntity(entity);
        _context.ProbeControlCommands.Update(entity);
    }

    private async Task<ProbeControlCommandEntity> GetRequiredEntityAsync(
        string commandId,
        CancellationToken cancellationToken)
    {
        return await _context.ProbeControlCommands
            .FirstOrDefaultAsync(e => e.CommandId == commandId, cancellationToken)
            ?? throw new InvalidOperationException($"Probe control command {commandId} not found");
    }
}
