namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class SetProbeTestEnabledUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProbeTestEnabledUseCase(
        IProbeRepository probeRepository,
        IProbeTestConfigurationRepository configRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updated.ToDto();
    }
}
