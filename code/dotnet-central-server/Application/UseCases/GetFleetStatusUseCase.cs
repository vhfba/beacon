namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Repositories;
public class GetFleetStatusUseCase
{
    private readonly IProbeRepository _probeRepository;

    public GetFleetStatusUseCase(IProbeRepository probeRepository)
    {
        _probeRepository = probeRepository;
    }

    public async Task<List<ProbeDTO>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var probes = await _probeRepository.GetAllAsync(cancellationToken: cancellationToken);
        return probes.Select(probe => new ProbeDTO
        {
            Id = probe.Id.Value,
            Name = probe.Name,
            Location = probe.Location,
            IpAddress = probe.IpAddress,
            Status = probe.Status.ToString(),
            CreatedAt = probe.CreatedAt,
            LastHeartbeat = probe.LastHeartbeat,
            LastConfigFetch = probe.LastConfigFetch
        }).ToList();
    }
}
