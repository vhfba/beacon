namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Repositories;

public class ListProbeControlCommandsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeControlCommandRepository _commandRepository;

    public ListProbeControlCommandsUseCase(
        IProbeRepository probeRepository,
        IProbeControlCommandRepository commandRepository)
    {
        _probeRepository = probeRepository;
        _commandRepository = commandRepository;
    }

    public async Task<IReadOnlyList<ProbeControlCommandDTO>> ExecuteAsync(
        string probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken, trim: true);
        var commands = await _commandRepository.GetByProbeIdAsync(probe.Id, limit, cancellationToken);
        return commands.Select(command => command.ToDto(redactSensitivePayload: true)).ToList();
    }
}
