namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;
public interface IProbeTestConfigurationRepository
{
    Task<IReadOnlyList<ProbeTestConfiguration>> GetByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProbeTestConfiguration>> GetEnabledByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default);

    Task<ProbeTestConfiguration?> GetAsync(ProbeId probeId, string testTypeName, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProbeTestConfiguration config, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProbeId probeId, string testTypeName, CancellationToken cancellationToken = default);
}
