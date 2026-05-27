namespace CentralServer.Application.UseCases;

using System.Globalization;
using System.Text;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;

public class ExportPrometheusMetricsUseCase
{
    private readonly IProbeMetricsStore _probeMetricsStore;

    public ExportPrometheusMetricsUseCase(IProbeMetricsStore probeMetricsStore)
    {
        _probeMetricsStore = probeMetricsStore;
    }

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await _probeMetricsStore.GetAllAsync(cancellationToken);
        var allSamples = snapshots
            .OrderBy(snapshot => snapshot.ProbeId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(snapshot => snapshot.Samples.Select(sample => new ProbeSample(snapshot.ProbeId, sample)))
            .GroupBy(item => item.Sample.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        var sb = new StringBuilder();
        foreach (var group in allSamples)
        {
            sb.Append("# TYPE ").Append(group.Key).Append(' ').Append(group.First().Sample.Kind).AppendLine();
            foreach (var sample in group.OrderBy(item => SerializeLabels(item.ProbeId, item.Sample), StringComparer.Ordinal))
            {
                sb.Append(sample.Sample.Name);
                sb.Append(SerializeLabels(sample.ProbeId, sample.Sample));
                sb.Append(' ');
                sb.Append(sample.Sample.Value.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string SerializeLabels(string probeId, MetricSampleInput sample)
    {
        var labels = new Dictionary<string, string>(sample.Labels, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(probeId) && !labels.ContainsKey("probe_id"))
        {
            labels["probe_id"] = probeId;
        }

        if (labels.Count == 0)
        {
            return string.Empty;
        }

        var parts = labels
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}=\"{EscapeLabelValue(pair.Value)}\"");
        return "{" + string.Join(",", parts) + "}";
    }

    private static string EscapeLabelValue(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private readonly record struct ProbeSample(string ProbeId, MetricSampleInput Sample);
}
