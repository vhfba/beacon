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

        var testType = await ResolveScheduledTestTypeAsync(input.TestType, cancellationToken);

        var config = new ProbeTestConfiguration(probe.Id, testType, input.IntervalSeconds, input.Enabled);
        await _configRepository.UpdateAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config.ToDto();
    }

    private async Task<TestType> ResolveScheduledTestTypeAsync(string testTypeName, CancellationToken cancellationToken)
    {
        if (BuiltInTestTypeDescriptions.TryGetValue(testTypeName, out var description))
        {
            return new TestType(testTypeName, description);
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

        return new TestType(plugin.Id, plugin.Description ?? $"Scheduled plugin check {plugin.Name}");
    }

    private static readonly IReadOnlyDictionary<string, string> BuiltInTestTypeDescriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RSSI"] = "Receive Signal Strength Indicator measurement",
            ["PING"] = "ICMP echo request to measure latency",
            ["HTTP"] = "HTTP connectivity and response time test",
            ["IPERF"] = "Network throughput measurement"
        };
}
