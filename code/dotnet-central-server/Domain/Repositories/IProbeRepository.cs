namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;
public interface IProbeRepository
{
    Task<Probe> RegisterAsync(Probe probe, CancellationToken cancellationToken = default);

    Task<Probe?> GetByIdAsync(ProbeId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Probe>> GetAllAsync(ProbeStatus? status = null, CancellationToken cancellationToken = default);

    Task<Probe?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task UpdateAsync(Probe probe, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProbeId id, CancellationToken cancellationToken = default);
}
