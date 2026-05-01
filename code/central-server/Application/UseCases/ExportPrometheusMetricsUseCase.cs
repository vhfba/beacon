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
            .SelectMany(snapshot => snapshot.Samples)
            .GroupBy(sample => sample.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        var sb = new StringBuilder();
        foreach (var group in allSamples)
        {
            sb.Append("# TYPE ").Append(group.Key).Append(' ').Append(group.First().Kind).AppendLine();
            foreach (var sample in group.OrderBy(SerializeLabels, StringComparer.Ordinal))
            {
                sb.Append(sample.Name);
                sb.Append(SerializeLabels(sample));
                sb.Append(' ');
                sb.Append(sample.Value.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string SerializeLabels(MetricSampleInput sample)
    {
        if (sample.Labels.Count == 0)
        {
            return string.Empty;
        }

        var parts = sample.Labels
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
}
