namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
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
        return probes.Select(probe => probe.ToDto()).ToList();
    }
}
