namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdateProbeTestConfigUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly ITestTypeRepository _testTypeRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeTestConfigUseCase(
        IProbeRepository probeRepository,
        ITestTypeRepository testTypeRepository,
        IProbeTestConfigurationRepository configRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _testTypeRepository = testTypeRepository;
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeTestConfigurationDTO> ExecuteAsync(
        UpdateProbeTestConfigInput input,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken);

        var testType = await _testTypeRepository.GetByNameAsync(input.TestType, cancellationToken);
        if (testType == null)
            throw new DomainException($"Test type {input.TestType} not found");

        var config = new ProbeTestConfiguration(probe.Id, testType, input.IntervalSeconds, input.Enabled);
        await _configRepository.UpdateAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config.ToDto();
    }
}
