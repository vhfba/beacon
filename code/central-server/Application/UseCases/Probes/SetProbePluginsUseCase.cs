namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class SetProbePluginsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IPluginRepository _pluginRepository;
    private readonly IProbePluginAssignmentRepository _assignmentRepository;
    private readonly IProbeTestConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProbePluginsUseCase(
        IProbeRepository probeRepository,
        IPluginRepository pluginRepository,
        IProbePluginAssignmentRepository assignmentRepository,
        IProbeTestConfigurationRepository configRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _pluginRepository = pluginRepository;
        _assignmentRepository = assignmentRepository;
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProbePluginAssignmentDTO>> ExecuteAsync(
        SetProbePluginsInput input,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken);
        var normalizedIds = UseCaseGuards.NormalizePluginIds(input.PluginIds);

        foreach (var pluginId in normalizedIds)
        {
            var plugin = await _pluginRepository.GetByIdAsync(pluginId, cancellationToken);
            if (plugin == null)
                throw new DomainException($"Plugin {pluginId} not found");
        }

        var existingConfigs = await _configRepository.GetByProbeIdAsync(probe.Id, cancellationToken);
        foreach (var config in existingConfigs.Where(c => c.Enabled && !normalizedIds.Contains(c.PluginId)))
        {
            await _configRepository.UpdateAsync(config.WithEnabled(false), cancellationToken);
        }

        await _assignmentRepository.SetForProbeAsync(probe.Id, normalizedIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var assignments = await _assignmentRepository.GetByProbeIdAsync(probe.Id, cancellationToken);
        var plugins = await _pluginRepository.GetAllAsync(cancellationToken);
        var pluginLookup = plugins.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        return assignments
            .Where(a => pluginLookup.ContainsKey(a.PluginId))
            .Select(a => a.ToDto(pluginLookup[a.PluginId]))
            .OrderBy(a => a.PluginName)
            .ThenBy(a => a.PluginVersion)
            .ToList();
    }
}
