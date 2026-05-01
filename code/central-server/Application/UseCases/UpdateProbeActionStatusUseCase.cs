namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdateProbeActionStatusUseCase
{
    private readonly IProbeActionExecutionRepository _probeActionExecutionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeActionStatusUseCase(IProbeActionExecutionRepository probeActionExecutionRepository, IUnitOfWork unitOfWork)
    {
        _probeActionExecutionRepository = probeActionExecutionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeActionExecutionDTO> ExecuteAsync(
        UpdateProbeActionStatusInput input,
        CancellationToken cancellationToken = default)
    {
        var execution = await _probeActionExecutionRepository.GetByIdAsync(input.ExecutionId, cancellationToken)
            ?? throw new DomainException($"Action execution {input.ExecutionId} not found");

        if (!string.Equals(execution.ProbeId.Value, input.ProbeId.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Action execution does not belong to provided probe");

        var now = DateTime.UtcNow;
        switch (input.Status)
        {
            case ProbeActionExecutionStatus.Running:
                execution.MarkRunning(now);
                break;
            case ProbeActionExecutionStatus.Succeeded:
                execution.MarkSucceeded(now);
                break;
            case ProbeActionExecutionStatus.Failed:
                execution.MarkFailed(now, input.ErrorMessage);
                break;
            case ProbeActionExecutionStatus.TimedOut:
                execution.MarkTimedOut(now, input.ErrorMessage);
                break;
            default:
                throw new DomainException($"Unsupported status transition target: {input.Status}");
        }

        await _probeActionExecutionRepository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return execution.ToDto();
    }
}
