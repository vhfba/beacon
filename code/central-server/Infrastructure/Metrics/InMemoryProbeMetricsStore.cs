namespace CentralServer.Infrastructure.Metrics;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;

public sealed class InMemoryProbeMetricsStore : IProbeMetricsStore
{
    private readonly Dictionary<string, ProbeMetricsSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();

    public Task StoreProbeMetricsAsync(string probeId, IReadOnlyList<MetricSampleInput> samples, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _snapshots[probeId] = new ProbeMetricsSnapshot(probeId, receivedAtUtc, samples);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProbeMetricsSnapshot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ProbeMetricsSnapshot>>(_snapshots.Values.ToList());
        }
    }
}
