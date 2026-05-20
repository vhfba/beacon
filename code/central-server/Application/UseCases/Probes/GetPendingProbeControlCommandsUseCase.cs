namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Repositories;

public class GetPendingProbeControlCommandsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeControlCommandRepository _commandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingProbeControlCommandsUseCase(
        IProbeRepository probeRepository,
        IProbeControlCommandRepository commandRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _commandRepository = commandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProbeControlCommandDTO>> ExecuteAsync(
        string probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken, trim: true);
        var commands = await _commandRepository.ClaimPendingForProbeAsync(probe.Id, limit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return commands.Select(command => command.ToDto(redactSensitivePayload: false)).ToList();
    }
}
