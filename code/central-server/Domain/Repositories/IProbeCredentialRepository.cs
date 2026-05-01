namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;

public interface IProbeCredentialRepository
{
    Task<ProbeCredential?> GetByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default);

    Task<ProbeCredential?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<ProbeCredential> UpsertAsync(ProbeCredential credential, CancellationToken cancellationToken = default);

    Task TouchAsync(ProbeId probeId, DateTime usedAt, CancellationToken cancellationToken = default);
}
