namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class SetPluginAvailabilityUseCase
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetPluginAvailabilityUseCase(IPluginRepository pluginRepository, IUnitOfWork unitOfWork)
    {
        _pluginRepository = pluginRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return plugin.ToDto();
    }
}
