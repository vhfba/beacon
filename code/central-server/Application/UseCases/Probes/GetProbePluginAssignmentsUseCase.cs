namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class GetProbePluginAssignmentsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IPluginRepository _pluginRepository;
    private readonly IProbePluginAssignmentRepository _assignmentRepository;

    public GetProbePluginAssignmentsUseCase(
        IProbeRepository probeRepository,
        IPluginRepository pluginRepository,
        IProbePluginAssignmentRepository assignmentRepository)
    {
        _probeRepository = probeRepository;
        _pluginRepository = pluginRepository;
        _assignmentRepository = assignmentRepository;
    }

    public async Task<IReadOnlyList<ProbePluginAssignmentDTO>> ExecuteAsync(
        string probeId,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken);

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
