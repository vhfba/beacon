namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class SetPluginAvailabilityUseCase
{
    private readonly IPluginRepository _pluginRepository;

    public SetPluginAvailabilityUseCase(IPluginRepository pluginRepository)
    {
        _pluginRepository = pluginRepository;
    }

    public async Task<PluginDTO> ExecuteAsync(
        SetPluginAvailabilityInput input,
        CancellationToken cancellationToken = default)
    {
        var plugin = await _pluginRepository.GetByIdAsync(input.PluginId, cancellationToken);
        if (plugin == null)
            throw new DomainException($"Plugin {input.PluginId} not found");

        if (input.Available)
        {
            plugin.Restore();
        }
        else
        {
            plugin.Retire();
        }

        await _pluginRepository.UpdateAsync(plugin, cancellationToken);

        return PluginDTO.FromDomain(plugin);
    }
}
