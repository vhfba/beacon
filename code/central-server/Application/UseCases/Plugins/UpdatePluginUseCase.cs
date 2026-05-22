namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdatePluginUseCase
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePluginUseCase(IPluginRepository pluginRepository, IUnitOfWork unitOfWork)
    {
        _pluginRepository = pluginRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PluginDTO> ExecuteAsync(UpdatePluginInput input, CancellationToken cancellationToken = default)
    {
        var currentId = input.CurrentId.Trim();
        var current = await _pluginRepository.GetByIdAsync(currentId, cancellationToken);
        if (current == null)
            throw new DomainException($"Plugin with ID {currentId} was not found");

        if (!string.Equals(current.Id, input.Id, StringComparison.OrdinalIgnoreCase))
        {
            var existingById = await _pluginRepository.GetByIdAsync(input.Id, cancellationToken);
            if (existingById != null)
                throw new DomainException($"Plugin with ID {input.Id} already exists");
        }

        var existingByName = await _pluginRepository.GetByNameAsync(input.Name, cancellationToken);
        if (existingByName.Any(p =>
                !string.Equals(p.Id, current.Id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.Version, input.Version, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Plugin {input.Name} version {input.Version} already exists");

        var updated = current.WithCatalogDetails(
            input.Id,
            input.Name,
            input.Version,
            input.Checksum,
            input.Description,
            input.BundleDownloadUrl,
            input.DashboardJson,
            input.ExecutionMode);

        await _pluginRepository.UpdateAsync(current.Id, updated, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updated.ToDto();
    }
}
