namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;

public interface IProbeActionExecutionRepository
{
    Task<ProbeActionExecution> CreateAsync(ProbeActionExecution execution, CancellationToken cancellationToken = default);

    Task<ProbeActionExecution?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProbeActionExecution>> ClaimPendingForProbeAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProbeActionExecution>> GetByProbeIdAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ProbeActionExecution execution, CancellationToken cancellationToken = default);
}
