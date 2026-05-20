namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;

public interface IProbeControlCommandRepository
{
    Task<ProbeControlCommand> CreateAsync(ProbeControlCommand command, CancellationToken cancellationToken = default);

    Task<ProbeControlCommand?> GetByIdAsync(string commandId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProbeControlCommand>> ClaimPendingForProbeAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProbeControlCommand>> GetByProbeIdAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ProbeControlCommand command, CancellationToken cancellationToken = default);
}
