namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class DeletePluginUseCase
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePluginUseCase(IPluginRepository pluginRepository, IUnitOfWork unitOfWork)
    {
        _pluginRepository = pluginRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var existing = await _pluginRepository.GetByIdAsync(pluginId, cancellationToken);
        if (existing == null)
            throw new DomainException($"Plugin {pluginId} not found");

        await _pluginRepository.DeleteAsync(pluginId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
