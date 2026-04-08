namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;
public interface IPluginRepository
{
    Task<Plugin?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plugin>> GetAvailableAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plugin>> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<Plugin?> GetLatestByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<Plugin> CreateAsync(Plugin plugin, CancellationToken cancellationToken = default);

    Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default);
}
