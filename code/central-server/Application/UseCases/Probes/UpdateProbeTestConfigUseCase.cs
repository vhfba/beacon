namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdateProbeTestConfigUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IPluginRepository _pluginRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeTestConfigUseCase(
        IProbeRepository probeRepository,
        IPluginRepository pluginRepository,
        IProbeTestConfigurationRepository configRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _pluginRepository = pluginRepository;
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeTestConfigurationDTO> ExecuteAsync(
        UpdateProbeTestConfigInput input,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken);

        var pluginId = await ResolveScheduledPluginIdAsync(input.TestType, cancellationToken);

        var config = new ProbeTestConfiguration(probe.Id, pluginId, input.IntervalSeconds, input.Enabled);
        await _configRepository.UpdateAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config.ToDto();
    }

    private async Task<string> ResolveScheduledPluginIdAsync(string pluginId, CancellationToken cancellationToken)
    {
        var plugin = await _pluginRepository.GetByIdAsync(pluginId, cancellationToken);
        if (plugin == null)
        {
            throw new DomainException($"Plugin {pluginId} not found");
        }

        if (plugin.ExecutionMode == PluginExecutionMode.Action)
        {
            throw new DomainException($"Plugin {pluginId} does not support scheduled execution");
        }

        if (!plugin.Available)
        {
            throw new DomainException($"Plugin {pluginId} is not available");
        }

        return plugin.Id;
    }
}
