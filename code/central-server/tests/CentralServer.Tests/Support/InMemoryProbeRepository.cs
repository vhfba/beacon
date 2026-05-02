namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryProbeRepository : IProbeRepository
{
    private readonly Dictionary<string, Probe> _probes = new(StringComparer.OrdinalIgnoreCase);

    public Task<Probe> RegisterAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        _probes[probe.Id.Value] = probe;
        return Task.FromResult(probe);
    }

    public Task<Probe?> GetByIdAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        _probes.TryGetValue(id.Value, out var probe);
        return Task.FromResult(probe);
    }

    public Task<IReadOnlyList<Probe>> GetAllAsync(ProbeStatus? status = null, CancellationToken cancellationToken = default)
    {
        var values = status is null
            ? _probes.Values.ToList()
            : _probes.Values.Where(p => p.Status == status.Value).ToList();

        return Task.FromResult<IReadOnlyList<Probe>>(values);
    }

    public Task<Probe?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var probe = _probes.Values.FirstOrDefault(p => string.Equals(p.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(probe);
    }

    public Task UpdateAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        _probes[probe.Id.Value] = probe;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        _probes.Remove(id.Value);
        return Task.CompletedTask;
    }
}
