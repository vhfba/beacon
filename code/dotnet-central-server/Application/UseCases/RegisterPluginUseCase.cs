namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class RegisterPluginUseCase
{
    private readonly IPluginRepository _pluginRepository;

    public RegisterPluginUseCase(IPluginRepository pluginRepository)
    {
        _pluginRepository = pluginRepository;
    }

    public async Task<PluginDTO> ExecuteAsync(RegisterPluginInput input, CancellationToken cancellationToken = default)
    {
        var existingById = await _pluginRepository.GetByIdAsync(input.Id, cancellationToken);
        if (existingById != null)
            throw new DomainException($"Plugin with ID {input.Id} already exists");

        var existingByName = await _pluginRepository.GetByNameAsync(input.Name, cancellationToken);
        if (existingByName.Any(p => string.Equals(p.Version, input.Version, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Plugin {input.Name} version {input.Version} already exists");

        var plugin = new Plugin(input.Id, input.Name, input.Version, input.Checksum, input.Description);
        var created = await _pluginRepository.CreateAsync(plugin, cancellationToken);

        return new PluginDTO
        {
            Id = created.Id,
            Name = created.Name,
            Version = created.Version,
            Checksum = created.Checksum,
            Description = created.Description,
            ReleasedAt = created.ReleasedAt,
            Available = created.Available
        };
    }
}
