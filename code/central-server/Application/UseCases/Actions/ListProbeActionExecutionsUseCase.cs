namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class ListProbeActionExecutionsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeActionExecutionRepository _probeActionExecutionRepository;

    public ListProbeActionExecutionsUseCase(
        IProbeRepository probeRepository,
        IProbeActionExecutionRepository probeActionExecutionRepository)
    {
        _probeRepository = probeRepository;
        _probeActionExecutionRepository = probeActionExecutionRepository;
    }

    public async Task<IReadOnlyList<ProbeActionExecutionDTO>> ExecuteAsync(
        string probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken, trim: true);

        var executions = await _probeActionExecutionRepository.GetByProbeIdAsync(probe.Id, limit, cancellationToken);
        return executions.Select(execution => execution.ToDto()).ToList();
    }
}
