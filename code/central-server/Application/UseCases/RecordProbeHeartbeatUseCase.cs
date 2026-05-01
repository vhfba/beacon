namespace CentralServer.Application.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Application.Services;

public class RecordProbeHeartbeatUseCase
{
    private readonly ProbeRuntimeCoordinator _probeRuntimeCoordinator;
    private readonly GetProbeRuntimeUseCase _getProbeRuntimeUseCase;

    public RecordProbeHeartbeatUseCase(
        ProbeRuntimeCoordinator probeRuntimeCoordinator,
        GetProbeRuntimeUseCase getProbeRuntimeUseCase)
    {
        _probeRuntimeCoordinator = probeRuntimeCoordinator;
        _getProbeRuntimeUseCase = getProbeRuntimeUseCase;
    }

    public async Task<ProbeHeartbeatResultDTO> ExecuteAsync(ProbeHeartbeatInput input, CancellationToken cancellationToken = default)
    {
        var (probe, autoRegistered) = await _probeRuntimeCoordinator.RecordHeartbeatAsync(input, cancellationToken);
        var runtime = await _getProbeRuntimeUseCase.ExecuteAsync(probe.Id.Value, cancellationToken);
        return new ProbeHeartbeatResultDTO
        {
            AutoRegistered = autoRegistered,
            Probe = probe.ToDto(),
            Runtime = runtime
        };
    }
}
