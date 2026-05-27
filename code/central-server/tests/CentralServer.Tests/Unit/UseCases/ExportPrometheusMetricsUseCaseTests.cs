namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;

public class ExportPrometheusMetricsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_SortsLabelsAndEscapesLabelValues()
    {
        var useCase = new ExportPrometheusMetricsUseCase(new FakeMetricsStore([
            new ProbeMetricsSnapshot("probe-a", DateTimeOffset.UtcNow, [
                new MetricSampleInput
                {
                    Name = "beacon_contract_metric",
                    Kind = "gauge",
                    Value = 12.5,
                    Labels = new Dictionary<string, string>
                    {
                        ["site"] = "Building \"A\"",
                        ["probe_id"] = "probe\\a",
                        ["target"] = "line\nbreak"
                    }
                }
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.Contains("# TYPE beacon_contract_metric gauge", result, StringComparison.Ordinal);
        Assert.Contains("beacon_contract_metric{probe_id=\"probe\\\\a\",site=\"Building \\\"A\\\"\",target=\"line\\nbreak\"} 12.5", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_AddsProbeIdWhenMissingFromLabels()
    {
        var useCase = new ExportPrometheusMetricsUseCase(new FakeMetricsStore([
            new ProbeMetricsSnapshot("probe-c", DateTimeOffset.UtcNow, [
                new MetricSampleInput
                {
                    Name = "beacon_dns_latency_ms",
                    Kind = "gauge",
                    Value = 5.62,
                    Labels = new Dictionary<string, string>
                    {
                        ["domain"] = "google.com",
                        ["resolver"] = "cloudflare"
                    }
                }
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.Contains("beacon_dns_latency_ms{domain=\"google.com\",probe_id=\"probe-c\",resolver=\"cloudflare\"} 5.62", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_OrdersMetricFamiliesAndSamplesDeterministically()
    {
        var useCase = new ExportPrometheusMetricsUseCase(new FakeMetricsStore([
            new ProbeMetricsSnapshot("probe-b", DateTimeOffset.UtcNow, [
                Sample("z_metric", "probe-b"),
                Sample("a_metric", "probe-b")
            ]),
            new ProbeMetricsSnapshot("probe-a", DateTimeOffset.UtcNow, [
                Sample("a_metric", "probe-a")
            ])
        ]));

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IndexOf("# TYPE a_metric", StringComparison.Ordinal) < result.IndexOf("# TYPE z_metric", StringComparison.Ordinal));
        Assert.True(result.IndexOf("probe_id=\"probe-a\"", StringComparison.Ordinal) < result.IndexOf("probe_id=\"probe-b\"", StringComparison.Ordinal));
    }

    private static MetricSampleInput Sample(string name, string probeId)
    {
        return new MetricSampleInput
        {
            Name = name,
            Kind = "gauge",
            Value = 1,
            Labels = new Dictionary<string, string> { ["probe_id"] = probeId }
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
