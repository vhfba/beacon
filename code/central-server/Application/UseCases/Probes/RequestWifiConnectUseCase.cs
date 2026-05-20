namespace CentralServer.Application.UseCases;

using System.Text.Json;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class RequestWifiConnectUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeControlCommandRepository _commandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RequestWifiConnectUseCase(
        IProbeRepository probeRepository,
        IProbeControlCommandRepository commandRepository,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _commandRepository = commandRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeControlCommandDTO> ExecuteAsync(RequestWifiConnectInput input, CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, input.ProbeId, cancellationToken, trim: true);
        if (string.IsNullOrWhiteSpace(input.Ssid))
            throw new DomainException("SSID is required");

        var payloadJson = JsonSerializer.Serialize(new
        {
            ssid = input.Ssid.Trim(),
            password = input.Password ?? string.Empty
        });
        var command = new ProbeControlCommand(probe.Id, ProbeControlCommandType.ConnectWifi, input.RequestedBy, payloadJson);
        var created = await _commandRepository.CreateAsync(command, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created.ToDto(redactSensitivePayload: true);
    }
}
