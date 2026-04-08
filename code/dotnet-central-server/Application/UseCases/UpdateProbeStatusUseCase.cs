namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
public class UpdateProbeStatusUseCase
{
    private readonly IProbeRepository _probeRepository;

    public UpdateProbeStatusUseCase(IProbeRepository probeRepository)
    {
        _probeRepository = probeRepository;
    }

    public async Task<ProbeDTO> ExecuteAsync(string probeId, string newStatus, CancellationToken cancellationToken = default)
    {
        var probe = await _probeRepository.GetByIdAsync(new ProbeId(probeId), cancellationToken);
        if (probe == null)
            throw new DomainException($"Probe {probeId} not found");

        if (!Enum.TryParse<ProbeStatus>(newStatus, true, out var status))
            throw new DomainException($"Invalid probe status: {newStatus}");

        probe.UpdateStatus(status);
        await _probeRepository.UpdateAsync(probe, cancellationToken);

        return new ProbeDTO
        {
            Id = probe.Id.Value,
            Name = probe.Name,
            Location = probe.Location,
            IpAddress = probe.IpAddress,
            Status = probe.Status.ToString(),
            CreatedAt = probe.CreatedAt,
            LastHeartbeat = probe.LastHeartbeat,
            LastConfigFetch = probe.LastConfigFetch
        };
    }
}
