namespace CentralServer.Application.UseCases;

using System.Text.Json;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdateProbeProfileUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeControlCommandRepository _commandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeProfileUseCase(
        IProbeRepository probeRepository,
        IProbeControlCommandRepository commandRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _commandRepository = commandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(ProbeDTO Probe, ProbeControlCommandDTO Command)> ExecuteAsync(
        UpdateProbeProfileInput input,
        CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken, trim: true);
        probe.UpdateProfile(input.Name, input.Location);
        await _probeRepository.UpdateAsync(probe, cancellationToken);

        var payloadJson = JsonSerializer.Serialize(new
        {
            name = input.Name.Trim(),
            location = input.Location.Trim()
        });
        var command = new ProbeControlCommand(probe.Id, ProbeControlCommandType.UpdateProfile, input.RequestedBy, payloadJson);
        var created = await _commandRepository.CreateAsync(command, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (probe.ToDto(), created.ToDto(redactSensitivePayload: true));
    }
}
