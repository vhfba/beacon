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
        var parsedProbeId = new ProbeId(probeId.Trim());
        var probe = await _probeRepository.GetByIdAsync(parsedProbeId, cancellationToken)
            ?? throw new DomainException($"Probe {probeId} not found");

        var enabledTests = await _probeTestConfigurationRepository.GetEnabledByProbeIdAsync(parsedProbeId, cancellationToken);
        return probe.ToRuntimeDto(enabledTests);
    }
}
