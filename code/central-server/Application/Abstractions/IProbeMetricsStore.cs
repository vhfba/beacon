namespace CentralServer.Application.Abstractions;

using CentralServer.Application.DTOs;

public interface IProbeMetricsStore
{
    Task StoreProbeMetricsAsync(string probeId, IReadOnlyList<MetricSampleInput> samples, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProbeMetricsSnapshot>> GetAllAsync(CancellationToken cancellationToken = default);
}

public record ProbeMetricsSnapshot(
    string ProbeId,
    DateTimeOffset ReceivedAtUtc,
    IReadOnlyList<MetricSampleInput> Samples);
