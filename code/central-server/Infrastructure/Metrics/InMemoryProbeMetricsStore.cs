namespace CentralServer.Infrastructure.Metrics;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using Microsoft.Extensions.Options;

public sealed class InMemoryProbeMetricsStore : IProbeMetricsStore
{
    private readonly Dictionary<string, ProbeMetricsSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();
    private readonly int _ttlSeconds;

    public InMemoryProbeMetricsStore(IOptions<MetricsStoreOptions> options)
    {
        _ttlSeconds = options.Value.Redis.ProbeSnapshotTtlSeconds;
    }

    public Task StoreProbeMetricsAsync(string probeId, IReadOnlyList<MetricSampleInput> samples, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _snapshots[probeId] = new ProbeMetricsSnapshot(probeId, receivedAtUtc, samples);
            Sweep();
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProbeMetricsSnapshot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            Sweep();
            return Task.FromResult<IReadOnlyList<ProbeMetricsSnapshot>>(_snapshots.Values.ToList());
        }
    }

    private void Sweep()
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-_ttlSeconds);
        var toRemove = new List<string>();

        foreach (var kvp in _snapshots)
        {
            if (kvp.Value.ReceivedAtUtc < cutoff)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            _snapshots.Remove(key);
        }
    }
}
