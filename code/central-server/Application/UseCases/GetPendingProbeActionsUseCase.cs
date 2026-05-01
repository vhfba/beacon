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
        var parsedProbeId = new ProbeId(probeId.Trim());
        var probe = await _probeRepository.GetByIdAsync(parsedProbeId, cancellationToken);
        if (probe is null)
            throw new DomainException($"Probe {probeId} not found");

        var claimed = await _probeActionExecutionRepository.ClaimPendingForProbeAsync(parsedProbeId, limit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return claimed.Select(execution => execution.ToDto()).ToList();
    }
}
