namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class TriggerProbeActionUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IPluginRepository _pluginRepository;
    private readonly IProbePluginAssignmentRepository _probePluginAssignmentRepository;
    private readonly IProbeActionExecutionRepository _probeActionExecutionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TriggerProbeActionUseCase(
        IProbeRepository probeRepository,
        IPluginRepository pluginRepository,
        IProbePluginAssignmentRepository probePluginAssignmentRepository,
        IProbeActionExecutionRepository probeActionExecutionRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _pluginRepository = pluginRepository;
        _probePluginAssignmentRepository = probePluginAssignmentRepository;
        _probeActionExecutionRepository = probeActionExecutionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeActionExecutionDTO> ExecuteAsync(
        TriggerProbeActionInput input,
        CancellationToken cancellationToken = default)
    {
        var probeId = new ProbeId(input.ProbeId.Trim());
        var probe = await _probeRepository.GetByIdAsync(probeId, cancellationToken);
        if (probe is null)
            throw new DomainException($"Probe {input.ProbeId} not found");

        var pluginId = input.PluginId.Trim();
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new DomainException("Plugin ID is required");

        var plugin = await _pluginRepository.GetByIdAsync(pluginId, cancellationToken);
        if (plugin is null)
            throw new DomainException($"Plugin {pluginId} not found");

        if (!plugin.Available)
            throw new DomainException($"Plugin {pluginId} is not available");

        if (plugin.ExecutionMode != PluginExecutionMode.Action)
            throw new DomainException($"Plugin {pluginId} does not support action execution mode");

        var assignments = await _probePluginAssignmentRepository.GetByProbeIdAsync(probeId, cancellationToken);
        var isAssigned = assignments.Any(a => string.Equals(a.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        if (!isAssigned)
            throw new DomainException($"Plugin {pluginId} is not assigned to probe {probeId.Value}");

        var execution = new ProbeActionExecution(probeId, pluginId, input.TriggeredBy);
        var created = await _probeActionExecutionRepository.CreateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created.ToDto();
    }
}
