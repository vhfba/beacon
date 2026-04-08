namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Repositories;
public class ListPluginsUseCase
{
    private readonly IPluginRepository _pluginRepository;

    public ListPluginsUseCase(IPluginRepository pluginRepository)
    {
        _pluginRepository = pluginRepository;
    }

    public async Task<List<PluginDTO>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var plugins = await _pluginRepository.GetAllAsync(cancellationToken);
        return plugins.Select(plugin => new PluginDTO
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Version = plugin.Version,
            Checksum = plugin.Checksum,
            Description = plugin.Description,
            ReleasedAt = plugin.ReleasedAt,
            Available = plugin.Available
        }).ToList();
    }
}
