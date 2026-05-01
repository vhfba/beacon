namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class ReportProbeMetricsUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IProbeMetricsStore _probeMetricsStore;
    private readonly IUnitOfWork _unitOfWork;

    public ReportProbeMetricsUseCase(
        IProbeRepository probeRepository,
        IProbeMetricsStore probeMetricsStore,
        IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _probeMetricsStore = probeMetricsStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportProbeMetricsResultDTO> ExecuteAsync(ReportProbeMetricsInput input, CancellationToken cancellationToken = default)
    {
        var parsedProbeId = new ProbeId(input.ProbeId.Trim());
        var probe = await _probeRepository.GetByIdAsync(parsedProbeId, cancellationToken)
            ?? throw new DomainException($"Probe {input.ProbeId} not found");

        var acceptedSamples = input.Samples
            .Where(sample => !string.IsNullOrWhiteSpace(sample.Name))
            .Select(sample => sample with
            {
                Name = sample.Name.Trim(),
                Kind = NormalizeKind(sample.Kind),
                Labels = (sample.Labels ?? new Dictionary<string, string>())
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                    .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value ?? string.Empty, StringComparer.Ordinal)
            })
            .ToList();

        var receivedAtUtc = DateTimeOffset.UtcNow;
        await _probeMetricsStore.StoreProbeMetricsAsync(parsedProbeId.Value, acceptedSamples, receivedAtUtc, cancellationToken);

        probe.RecordMetricsPush();
        await _probeRepository.UpdateAsync(probe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReportProbeMetricsResultDTO
        {
            ProbeId = parsedProbeId.Value,
            AcceptedSamples = acceptedSamples.Count,
            ReceivedAtUtc = receivedAtUtc
        };
    }

    private static string NormalizeKind(string? kind)
    {
        return string.Equals(kind, "counter", StringComparison.OrdinalIgnoreCase)
            ? "counter"
            : "gauge";
    }
}
