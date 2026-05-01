namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class RegisterPluginUseCase
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPluginUseCase(IPluginRepository pluginRepository, IUnitOfWork unitOfWork)
    {
        _pluginRepository = pluginRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PluginDTO> ExecuteAsync(RegisterPluginInput input, CancellationToken cancellationToken = default)
    {
        var existingById = await _pluginRepository.GetByIdAsync(input.Id, cancellationToken);
        if (existingById != null)
            throw new DomainException($"Plugin with ID {input.Id} already exists");

        var existingByName = await _pluginRepository.GetByNameAsync(input.Name, cancellationToken);
        if (existingByName.Any(p => string.Equals(p.Version, input.Version, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Plugin {input.Name} version {input.Version} already exists");

        var plugin = new Plugin(
            input.Id,
            input.Name,
            input.Version,
            input.Checksum,
            input.Description,
            input.BundleDownloadUrl,
            input.DashboardJson,
            input.ExecutionMode);
        var created = await _pluginRepository.CreateAsync(plugin, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created.ToDto();
    }
}
