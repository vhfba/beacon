namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class GetProbeRuntimeUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeTestConfigurationRepository _probeTestConfigurationRepository;

    public GetProbeRuntimeUseCase(
        IProbeRepository probeRepository,
        IProbeTestConfigurationRepository probeTestConfigurationRepository)
    {
        _probeRepository = probeRepository;
        _probeTestConfigurationRepository = probeTestConfigurationRepository;
    }

    public async Task<ProbeRuntimeDTO> ExecuteAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken, trim: true);

        var enabledTests = await _probeTestConfigurationRepository.GetEnabledByProbeIdAsync(probe.Id, cancellationToken);
        return probe.ToRuntimeDto(enabledTests);
    }
}
