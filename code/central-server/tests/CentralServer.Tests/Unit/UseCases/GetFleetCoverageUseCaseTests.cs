namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;

public class GetFleetCoverageUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithStrongSignals_ReturnsExcellent()
    {
        var useCase = new GetFleetCoverageUseCase(new FakeMetricsStore([
            Snapshot("probe-a", [
                Sample("beacon_wifi_rssi_dbm", -55),
                Sample("beacon_wifi_snr_db", 34),
                Sample("beacon_wifi_link_quality_percent", 95),
                Sample("beacon_ping_latency_ms", 20),
                Sample("beacon_ping_packet_loss_percent", 0)
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.Single(result);
        Assert.Equal(100, result[0].Score);
        Assert.Equal("EXCELLENT", result[0].Grade);
    }

    [Fact]
    public async Task ExecuteAsync_WithWeakSignals_ReturnsWeak()
    {
        var useCase = new GetFleetCoverageUseCase(new FakeMetricsStore([
            Snapshot("probe-a", [
                Sample("beacon_wifi_rssi_dbm", -70),
                Sample("beacon_wifi_snr_db", 22),
                Sample("beacon_wifi_link_quality_percent", 70),
                Sample("beacon_ping_latency_ms", 90),
                Sample("beacon_ping_packet_loss_percent", 2)
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.Equal("WEAK", result[0].Grade);
        Assert.InRange(result[0].Score, 45, 69);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnusableSignals_ReturnsUnusable()
    {
        var useCase = new GetFleetCoverageUseCase(new FakeMetricsStore([
            Snapshot("probe-a", [
                Sample("beacon_wifi_rssi_dbm", -90),
                Sample("beacon_wifi_snr_db", 6),
                Sample("beacon_wifi_link_quality_percent", 25),
                Sample("beacon_ping_latency_ms", 220),
                Sample("beacon_ping_packet_loss_percent", 18)
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.Equal(0, result[0].Score);
        Assert.Equal("UNUSABLE", result[0].Grade);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCoverageSamples_ReturnsNoData()
    {
        var useCase = new GetFleetCoverageUseCase(new FakeMetricsStore([
            Snapshot("probe-a", [
                Sample("beacon_probe_runtime_can_emit_metrics", 1)
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.Equal(0, result[0].Score);
        Assert.Equal("NO_DATA", result[0].Grade);
    }

    private static ProbeMetricsSnapshot Snapshot(string probeId, IReadOnlyList<MetricSampleInput> samples)
    {
        return new ProbeMetricsSnapshot(probeId, DateTimeOffset.UtcNow, samples);
    }

    private static MetricSampleInput Sample(string name, double value)
    {
        return new MetricSampleInput
        {
            Name = name,
            Kind = "gauge",
            Value = value,
            Labels = new Dictionary<string, string>
            {
                ["site"] = "Building A"
            }
        };
    }

    private sealed class FakeMetricsStore : IProbeMetricsStore
    {
        private readonly IReadOnlyList<ProbeMetricsSnapshot> _snapshots;

        public FakeMetricsStore(IReadOnlyList<ProbeMetricsSnapshot> snapshots)
        {
            _snapshots = snapshots;
        }

        public Task StoreProbeMetricsAsync(string probeId, IReadOnlyList<MetricSampleInput> samples, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ProbeMetricsSnapshot>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshots);
        }
    }
}
