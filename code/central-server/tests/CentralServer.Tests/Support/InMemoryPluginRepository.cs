namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryPluginRepository : IPluginRepository
{
    private readonly Dictionary<string, Plugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public Task<Plugin?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _plugins.TryGetValue(id, out var plugin);
        return Task.FromResult(plugin);
    }

    public Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plugin>>(_plugins.Values.ToList());
    }

    public Task<IReadOnlyList<Plugin>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plugin>>(_plugins.Values.Where(p => p.Available).ToList());
    }

    public Task<IReadOnlyList<Plugin>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plugin>>(
            _plugins.Values.Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public Task<Plugin?> GetLatestByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var latest = _plugins.Values
            .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.ReleasedAt)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<Plugin> CreateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        _plugins[plugin.Id] = plugin;
        return Task.FromResult(plugin);
    }

    public Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        _plugins[plugin.Id] = plugin;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string currentId, Plugin plugin, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(currentId, plugin.Id, StringComparison.OrdinalIgnoreCase))
        {
            _plugins.Remove(currentId);
        }

        _plugins[plugin.Id] = plugin;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _plugins.Remove(id);
        return Task.CompletedTask;
    }
}
