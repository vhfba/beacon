namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class GetProbeRuntimeUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeTestConfigurationRepository _probeTestConfigurationRepository;
    private readonly IProbePluginAssignmentRepository _probePluginAssignmentRepository;

    public GetProbeRuntimeUseCase(
        IProbeRepository probeRepository,
        IProbeTestConfigurationRepository probeTestConfigurationRepository,
        IProbePluginAssignmentRepository probePluginAssignmentRepository)
    {
        _probeRepository = probeRepository;
        _probeTestConfigurationRepository = probeTestConfigurationRepository;
        _probePluginAssignmentRepository = probePluginAssignmentRepository;
    }

    public async Task<ProbeRuntimeDTO> ExecuteAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken, trim: true);

        var enabledTests = await _probeTestConfigurationRepository.GetEnabledByProbeIdAsync(probe.Id, cancellationToken);
        var assignments = await _probePluginAssignmentRepository.GetByProbeIdAsync(probe.Id, cancellationToken);
        var assignedPluginIds = assignments
            .Select(a => a.PluginId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        enabledTests = enabledTests
            .Where(config => assignedPluginIds.Contains(config.PluginId))
            .ToList();

        return probe.ToRuntimeDto(enabledTests);
    }
}
