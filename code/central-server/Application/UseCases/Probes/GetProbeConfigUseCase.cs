namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class GetProbeConfigUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;
    private readonly IPluginRepository _pluginRepository;
    private readonly IProbePluginAssignmentRepository _assignmentRepository;

    public GetProbeConfigUseCase(
        IProbeRepository probeRepository,
        IProbeTestConfigurationRepository configRepository,
        IPluginRepository pluginRepository,
        IProbePluginAssignmentRepository assignmentRepository)
    {
        _probeRepository = probeRepository;
        _configRepository = configRepository;
        _pluginRepository = pluginRepository;
        _assignmentRepository = assignmentRepository;
    }

    public async Task<ProbeConfigDTO> ExecuteAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken);

        var configs = await _configRepository.GetEnabledByProbeIdAsync(probe.Id, cancellationToken);
        var assignments = await _assignmentRepository.GetByProbeIdAsync(probe.Id, cancellationToken);
        var availablePlugins = await _pluginRepository.GetAvailableAsync(cancellationToken);

        return ApplicationDtoMappings.ToConfigDto(probeId, configs, assignments, availablePlugins);
    }
}
