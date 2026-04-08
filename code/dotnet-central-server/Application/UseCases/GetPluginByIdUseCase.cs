namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
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

        return new PluginDTO
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Version = plugin.Version,
            Checksum = plugin.Checksum,
            Description = plugin.Description,
            ReleasedAt = plugin.ReleasedAt,
            Available = plugin.Available
        };
    }
}
