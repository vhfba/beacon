namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
public class RegisterProbeUseCase
{
    private readonly IProbeRepository _probeRepository;

    public RegisterProbeUseCase(IProbeRepository probeRepository)
    {
        _probeRepository = probeRepository;
    }

    public async Task<ProbeDTO> ExecuteAsync(RegisterProbeInput input, CancellationToken cancellationToken = default)
    {
        var existingProbe = await _probeRepository.GetByIpAddressAsync(input.IpAddress, cancellationToken);
        if (existingProbe != null)
            throw new ProbeRegistrationException($"Probe with IP address {input.IpAddress} already registered");

        var probeId = new ProbeId(input.Id);
        var probe = new Probe(probeId, input.Name, input.Location, input.IpAddress);

        var registered = await _probeRepository.RegisterAsync(probe, cancellationToken);

        return new ProbeDTO
        {
            Id = registered.Id.Value,
            Name = registered.Name,
            Location = registered.Location,
            IpAddress = registered.IpAddress,
            Status = registered.Status.ToString(),
            CreatedAt = registered.CreatedAt,
            LastHeartbeat = registered.LastHeartbeat,
            LastConfigFetch = registered.LastConfigFetch
        };
    }
}
