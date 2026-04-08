namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
public class GetProbeConfigUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;

    public GetProbeConfigUseCase(
        IProbeRepository probeRepository,
        IProbeTestConfigurationRepository configRepository)
    {
        _probeRepository = probeRepository;
        _configRepository = configRepository;
    }

    public async Task<ProbeConfigDTO> ExecuteAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var probe = await _probeRepository.GetByIdAsync(new ProbeId(probeId), cancellationToken);
        if (probe == null)
            throw new DomainException($"Probe {probeId} not found");

        probe.RecordConfigFetch();
        await _probeRepository.UpdateAsync(probe, cancellationToken);

        var configs = await _configRepository.GetEnabledByProbeIdAsync(new ProbeId(probeId), cancellationToken);

        return new ProbeConfigDTO
        {
            ProbeId = probeId,
            EnabledTests = configs.Select(c => new ProbeTestConfigurationDTO
            {
                ProbeId = c.ProbeId.Value,
                TestType = c.TestType.Name,
                IntervalSeconds = c.IntervalSeconds,
                Enabled = c.Enabled
            }).ToList()
        };
    }
}
