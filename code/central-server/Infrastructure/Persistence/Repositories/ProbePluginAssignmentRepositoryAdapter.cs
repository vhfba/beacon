namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class ProbePluginAssignmentRepositoryAdapter : IProbePluginAssignmentRepository
{
    private readonly CentralServerDbContext _context;

    public ProbePluginAssignmentRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProbePluginAssignment>> GetByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.ProbePluginAssignments
            .Where(pa => pa.ProbeId == probeId.Value)
            .OrderBy(pa => pa.PluginId)
            .ToListAsync(cancellationToken);

        return entities
            .Select(pa => ProbePluginAssignment.Rehydrate(pa.ProbeId, pa.PluginId, pa.AssignedAt))
            .ToList();
    }

    public async Task SetForProbeAsync(ProbeId probeId, IReadOnlyCollection<string> pluginIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = pluginIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await _context.ProbePluginAssignments
            .Where(pa => pa.ProbeId == probeId.Value)
            .ToListAsync(cancellationToken);

        var existingLookup = existing
            .ToDictionary(pa => pa.PluginId, StringComparer.OrdinalIgnoreCase);

        foreach (var entity in existing)
        {
            if (!normalizedIds.Contains(entity.PluginId))
            {
                _context.ProbePluginAssignments.Remove(entity);
            }
        }

        var now = DateTime.UtcNow;
        foreach (var pluginId in normalizedIds)
        {
            if (existingLookup.ContainsKey(pluginId))
            {
                continue;
            }

            _context.ProbePluginAssignments.Add(new ProbePluginAssignmentEntity
            {
                ProbeId = probeId.Value,
                PluginId = pluginId,
                AssignedAt = now
            });
        }
    }
}
