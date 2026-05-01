namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Repositories;

public class GetPluginByIdUseCase
{
    private readonly IPluginRepository _pluginRepository;

    public GetPluginByIdUseCase(IPluginRepository pluginRepository)
    {
        _pluginRepository = pluginRepository;
    }

    public async Task<PluginDTO?> ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        var plugin = await _pluginRepository.GetByIdAsync(id, cancellationToken);
        if (plugin == null)
            return null;

        return plugin.ToDto();
    }
}
