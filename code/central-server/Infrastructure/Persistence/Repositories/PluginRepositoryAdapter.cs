namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
using CentralServer.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

public class PluginRepositoryAdapter : IPluginRepository
{
    private readonly CentralServerDbContext _context;

    public PluginRepositoryAdapter(CentralServerDbContext context)
    {
        _context = context;
    }

    public async Task<Plugin?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Plugins.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Plugins
            .OrderByDescending(p => p.ReleasedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Plugin>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Plugins.Where(p => p.Available).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Plugin>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Plugins
            .Where(p => p.Name == name)
            .OrderByDescending(p => p.ReleasedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async Task<Plugin?> GetLatestByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Plugins
            .Where(p => p.Name == name && p.Available)
            .OrderByDescending(p => p.ReleasedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity?.ToDomain();
    }

    public Task<Plugin> CreateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        var entity = plugin.ToEntity();
        _context.Plugins.Add(entity);
        return Task.FromResult(plugin);
    }

    public async Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(plugin.Id, cancellationToken);
        plugin.ApplyToEntity(entity);

        _context.Plugins.Update(entity);
    }

    public async Task UpdateAsync(string currentId, Plugin plugin, CancellationToken cancellationToken = default)
    {
        if (string.Equals(currentId, plugin.Id, StringComparison.Ordinal))
        {
            await UpdateAsync(plugin, cancellationToken);
            return;
        }

        var existingEntity = await GetRequiredEntityAsync(currentId, cancellationToken);
        var replacementEntity = plugin.ToEntity();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        existingEntity.Name = $"__renaming__{Guid.NewGuid():N}";
        existingEntity.Version = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync(cancellationToken);

        _context.Plugins.Add(replacementEntity);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.ProbePluginAssignments
            .Where(a => a.PluginId == currentId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.PluginId, plugin.Id), cancellationToken);

        await _context.ProbeActionExecutions
            .Where(a => a.PluginId == currentId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.PluginId, plugin.Id), cancellationToken);

        await _context.ProbeTestConfigurations
            .Where(c => c.TestType == currentId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.TestType, plugin.Id), cancellationToken);

        _context.Plugins.Remove(existingEntity);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(id, cancellationToken);
        var actionExecutions = await _context.ProbeActionExecutions
            .Where(e => e.PluginId == id)
            .ToListAsync(cancellationToken);
        var assignments = await _context.ProbePluginAssignments
            .Where(a => a.PluginId == id)
            .ToListAsync(cancellationToken);

        _context.ProbeActionExecutions.RemoveRange(actionExecutions);
        _context.ProbePluginAssignments.RemoveRange(assignments);
        _context.Plugins.Remove(entity);
    }

    private async Task<PluginEntity> GetRequiredEntityAsync(string id, CancellationToken cancellationToken)
    {
        return await _context.Plugins.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Plugin {id} not found");
    }
}
