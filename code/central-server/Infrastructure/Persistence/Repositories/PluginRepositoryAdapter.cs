namespace CentralServer.Infrastructure.Persistence.Repositories;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence.Entities;
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
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Plugins
            .OrderByDescending(p => p.ReleasedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<Plugin>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Plugins.Where(p => p.Available).ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<Plugin>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Plugins
            .Where(p => p.Name == name)
            .OrderByDescending(p => p.ReleasedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<Plugin?> GetLatestByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Plugins
            .Where(p => p.Name == name && p.Available)
            .OrderByDescending(p => p.ReleasedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<Plugin> CreateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        var entity = new PluginEntity
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Version = plugin.Version,
            Checksum = plugin.Checksum,
            Description = plugin.Description,
            ReleasedAt = plugin.ReleasedAt,
            Available = plugin.Available
        };

        _context.Plugins.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return plugin;
    }

    public async Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Plugins.FirstOrDefaultAsync(p => p.Id == plugin.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Plugin {plugin.Id} not found");

        entity.Available = plugin.Available;

        _context.Plugins.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Plugin MapToDomain(PluginEntity entity)
    {
        return Plugin.Rehydrate(
            entity.Id,
            entity.Name,
            entity.Version,
            entity.Checksum,
            entity.Description,
            entity.ReleasedAt,
            entity.Available
        );
    }
}
