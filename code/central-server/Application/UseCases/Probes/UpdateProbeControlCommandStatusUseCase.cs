namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdateProbeControlCommandStatusUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeControlCommandRepository _commandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeControlCommandStatusUseCase(
        IProbeRepository probeRepository,
        IProbeControlCommandRepository commandRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _commandRepository = commandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeControlCommandDTO> ExecuteAsync(
        UpdateProbeControlCommandStatusInput input,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken, trim: true);
        var command = await _commandRepository.GetByIdAsync(input.CommandId, cancellationToken)
            ?? throw new DomainException($"Probe control command {input.CommandId} not found");

        if (!string.Equals(command.ProbeId.Value, probe.Id.Value, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Probe control command {input.CommandId} does not belong to probe {probe.Id.Value}");

        var now = DateTime.UtcNow;
        switch (input.Status)
        {
            case ProbeControlCommandStatus.Running:
                command.MarkRunning(now);
                break;
            case ProbeControlCommandStatus.Succeeded:
                command.MarkSucceeded(now, input.ResultJson);
                break;
            case ProbeControlCommandStatus.Failed:
                command.MarkFailed(now, input.ErrorMessage);
                break;
            case ProbeControlCommandStatus.TimedOut:
                command.MarkFailed(now, input.ErrorMessage, timedOut: true);
                break;
            default:
                throw new DomainException($"Unsupported command status transition to {input.Status}");
        }

        await _commandRepository.UpdateAsync(command, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return command.ToDto(redactSensitivePayload: true);
    }
}
