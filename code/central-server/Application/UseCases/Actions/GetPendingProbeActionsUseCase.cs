namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class GetPendingProbeActionsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeActionExecutionRepository _probeActionExecutionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingProbeActionsUseCase(
        IProbeRepository probeRepository,
        IProbeActionExecutionRepository probeActionExecutionRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _probeActionExecutionRepository = probeActionExecutionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProbeActionExecutionDTO>> ExecuteAsync(
        string probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var activity = Diagnostics.ActivitySource.StartActivity("GetPendingProbeActions");
        activity?.SetTag("probe.id", probeId);
        activity?.SetTag("action.limit", limit);

        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken, trim: true);

        var claimed = await _probeActionExecutionRepository.ClaimPendingForProbeAsync(probe.Id, limit, cancellationToken);
        
        activity?.SetTag("action.claimed_count", claimed.Count);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return claimed.Select(execution => execution.ToDto()).ToList();
    }
}
