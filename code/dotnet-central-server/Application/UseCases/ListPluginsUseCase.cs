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
        return plugins.Select(PluginDTO.FromDomain).ToList();
    }
}
