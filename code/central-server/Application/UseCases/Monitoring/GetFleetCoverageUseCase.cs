namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;

public class GetFleetCoverageUseCase
{
    private readonly IProbeMetricsStore _probeMetricsStore;

    public GetFleetCoverageUseCase(IProbeMetricsStore probeMetricsStore)
    {
        _probeMetricsStore = probeMetricsStore;
    }

    public async Task<IReadOnlyList<ProbeCoverageSummaryDTO>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await _probeMetricsStore.GetAllAsync(cancellationToken);
        return snapshots
            .OrderBy(snapshot => snapshot.ProbeId, StringComparer.OrdinalIgnoreCase)
            .Select(ToCoverageSummary)
            .ToList();
    }

    private static ProbeCoverageSummaryDTO ToCoverageSummary(ProbeMetricsSnapshot snapshot)
    {
        var rssi = LatestValue(snapshot, "beacon_wifi_rssi_dbm");
        var snr = LatestValue(snapshot, "beacon_wifi_snr_db");
        var linkQuality = LatestValue(snapshot, "beacon_wifi_link_quality_percent");
        var pingLatency = LatestValue(snapshot, "beacon_ping_latency_ms");
        var pingLoss = LatestValue(snapshot, "beacon_ping_packet_loss_percent");

        var hasCoverageData = rssi.HasValue || snr.HasValue || linkQuality.HasValue || pingLatency.HasValue || pingLoss.HasValue;
        if (!hasCoverageData)
        {
            return new ProbeCoverageSummaryDTO
            {
                ProbeId = snapshot.ProbeId,
                Site = ResolveSite(snapshot),
                Grade = "NO_DATA",
                SampleCount = snapshot.Samples.Count,
                ReceivedAtUtc = snapshot.ReceivedAtUtc
            };
        }

        var penalty = 0.0;
        penalty += LinearPenalty(rssi, -60, -85, 35);
        penalty += LinearPenalty(snr, 30, 10, 25);
        penalty += LinearPenalty(linkQuality, 90, 40, 20);
        penalty += LinearPenalty(pingLatency, 40, 180, 10, lowerIsBetter: true);
        penalty += LinearPenalty(pingLoss, 0, 10, 10, lowerIsBetter: true);

        var score = Math.Clamp((int)Math.Round(100 - penalty, MidpointRounding.AwayFromZero), 0, 100);

        return new ProbeCoverageSummaryDTO
        {
            ProbeId = snapshot.ProbeId,
            Site = ResolveSite(snapshot),
            Score = score,
            Grade = ToGrade(score),
            RssiDbm = rssi,
            SnrDb = snr,
            LinkQualityPercent = linkQuality,
            PingLatencyMs = pingLatency,
            PingPacketLossPercent = pingLoss,
            SampleCount = snapshot.Samples.Count,
            ReceivedAtUtc = snapshot.ReceivedAtUtc
        };
    }

    private static double? LatestValue(ProbeMetricsSnapshot snapshot, string metricName)
    {
        return snapshot.Samples
            .LastOrDefault(sample => string.Equals(sample.Name, metricName, StringComparison.Ordinal))
            ?.Value;
    }

    private static string ResolveSite(ProbeMetricsSnapshot snapshot)
    {
        var site = snapshot.Samples
            .Select(sample => sample.Labels.TryGetValue("site", out var value) ? value : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return string.IsNullOrWhiteSpace(site) ? "default" : site;
    }

    private static double LinearPenalty(double? value, double good, double bad, double maxPenalty, bool lowerIsBetter = false)
    {
        if (!value.HasValue)
        {
            return 0;
        }

        if (lowerIsBetter)
        {
            if (value.Value <= good)
            {
                return 0;
            }

            if (value.Value >= bad)
            {
                return maxPenalty;
            }

            return ((value.Value - good) / (bad - good)) * maxPenalty;
        }

        if (value.Value >= good)
        {
            return 0;
        }

        if (value.Value <= bad)
        {
            return maxPenalty;
        }

        return ((good - value.Value) / (good - bad)) * maxPenalty;
    }

    private static string ToGrade(int score)
    {
        if (score >= 85)
        {
            return "EXCELLENT";
        }

        if (score >= 70)
        {
            return "GOOD";
        }

        return score >= 45 ? "WEAK" : "UNUSABLE";
    }
}
