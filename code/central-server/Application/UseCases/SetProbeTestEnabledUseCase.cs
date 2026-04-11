namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class SetProbeTestEnabledUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;

    public SetProbeTestEnabledUseCase(
        IProbeRepository probeRepository,
        IProbeTestConfigurationRepository configRepository)
    {
        _probeRepository = probeRepository;
        _configRepository = configRepository;
    }

    public async Task<ProbeTestConfigurationDTO> ExecuteAsync(
        SetProbeTestEnabledInput input,
        CancellationToken cancellationToken = default)
    {
        var probeId = new ProbeId(input.ProbeId);
        var probe = await _probeRepository.GetByIdAsync(probeId, cancellationToken);
        if (probe == null)
            throw new DomainException($"Probe {input.ProbeId} not found");

        var config = await _configRepository.GetAsync(probeId, input.TestType, cancellationToken);
        if (config == null)
            throw new DomainException($"Test config {input.TestType} for probe {input.ProbeId} not found");

        var updated = config.WithEnabled(input.Enabled);
        await _configRepository.UpdateAsync(updated, cancellationToken);

        return new ProbeTestConfigurationDTO
        {
            ProbeId = updated.ProbeId.Value,
            TestType = updated.TestType.Name,
            IntervalSeconds = updated.IntervalSeconds,
            Enabled = updated.Enabled
        };
    }
}
