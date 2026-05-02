namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using CentralServer.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

public class ProbeActionExecutionRepositoryAdapter : IProbeActionExecutionRepository
{
    private readonly CentralServerDbContext _context;

    public ProbeActionExecutionRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public Task<ProbeActionExecution> CreateAsync(ProbeActionExecution execution, CancellationToken cancellationToken = default)
    {
        var entity = execution.ToEntity();
        _context.ProbeActionExecutions.Add(entity);
        return Task.FromResult(execution);
    }

    public async Task<ProbeActionExecution?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProbeActionExecutions
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<ProbeActionExecution>> ClaimPendingForProbeAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);

        var entities = await _context.ProbeActionExecutions
            .Where(e => e.ProbeId == probeId.Value && e.Status == ProbeActionExecutionStatus.Queued)
            .OrderBy(e => e.RequestedAtUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.Status = ProbeActionExecutionStatus.Delivered;
            entity.DeliveredAtUtc = now;
        }

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<ProbeActionExecution>> GetByProbeIdAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var entities = await _context.ProbeActionExecutions
            .Where(e => e.ProbeId == probeId.Value)
            .OrderByDescending(e => e.RequestedAtUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task UpdateAsync(ProbeActionExecution execution, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(execution.ExecutionId, cancellationToken);
        execution.ApplyToEntity(entity);

        _context.ProbeActionExecutions.Update(entity);
    }

    private async Task<ProbeActionExecutionEntity> GetRequiredEntityAsync(
        string executionId,
        CancellationToken cancellationToken)
    {
        return await _context.ProbeActionExecutions
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken)
            ?? throw new InvalidOperationException($"Action execution {executionId} not found");
    }
}
