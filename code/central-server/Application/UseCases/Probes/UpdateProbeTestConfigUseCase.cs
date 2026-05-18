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
    private readonly IPluginRepository _pluginRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeTestConfigUseCase(
        IProbeRepository probeRepository,
        ITestTypeRepository testTypeRepository,
        IPluginRepository pluginRepository,
        IProbeTestConfigurationRepository configRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _testTypeRepository = testTypeRepository;
        _pluginRepository = pluginRepository;
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeTestConfigurationDTO> ExecuteAsync(
        UpdateProbeTestConfigInput input,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken);

        var testType = await ResolveScheduledTestTypeAsync(input.TestType, cancellationToken);

        var config = new ProbeTestConfiguration(probe.Id, testType, input.IntervalSeconds, input.Enabled);
        await _configRepository.UpdateAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config.ToDto();
    }

    private async Task<TestType> ResolveScheduledTestTypeAsync(string testTypeName, CancellationToken cancellationToken)
    {
        var testType = await _testTypeRepository.GetByNameAsync(testTypeName, cancellationToken);
        if (testType != null)
        {
            return testType;
        }

        var plugin = await _pluginRepository.GetByIdAsync(testTypeName, cancellationToken);
        if (plugin == null)
        {
            throw new DomainException($"Test type {testTypeName} not found");
        }

        if (plugin.ExecutionMode == PluginExecutionMode.Action)
        {
            throw new DomainException($"Plugin {testTypeName} does not support scheduled execution");
        }

        if (!plugin.Available)
        {
            throw new DomainException($"Plugin {testTypeName} is not available");
        }

        var pluginBackedTestType = new TestType(plugin.Id, plugin.Description ?? $"Scheduled plugin test {plugin.Name}");
        await _testTypeRepository.CreateAsync(pluginBackedTestType, cancellationToken);
        return pluginBackedTestType;
    }
}
