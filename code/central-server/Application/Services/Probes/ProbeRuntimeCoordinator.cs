namespace CentralServer.Application.Services;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class ProbeRuntimeCoordinator
{
    private readonly IProbeRepository _probeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProbeRuntimeCoordinator(IProbeRepository probeRepository, IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(Probe Probe, bool AutoRegistered)> RecordHeartbeatAsync(
        ProbeHeartbeatInput input,
        CancellationToken cancellationToken = default)
    {
        var parsedProbeId = new ProbeId(input.ProbeId.Trim());
        var probe = await _probeRepository.GetByIdAsync(parsedProbeId, cancellationToken);
        if (probe is null)
        {
            probe = new Probe(parsedProbeId, input.Name, input.Location, input.IpAddress, input.Ssid, input.AgentVersion);
            probe.RecordPassiveHeartbeat();
            probe = await _probeRepository.RegisterAsync(probe, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (probe, true);
        }

        if (probe.Status == ProbeStatus.Decommissioned)
        {
            throw new DomainException("Decommissioned probes cannot send heartbeat.");
        }

        probe.UpdateReportedDetails(input.Name, input.Location, input.IpAddress, input.Ssid, input.AgentVersion);
        probe.RecordPassiveHeartbeat();
        await _probeRepository.UpdateAsync(probe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (probe, false);
    }
}
