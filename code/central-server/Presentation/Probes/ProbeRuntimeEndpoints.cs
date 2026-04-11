namespace CentralServer.Presentation.Probes;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using CentralServer.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class ProbeRuntimeEndpoints
{
    public static IEndpointRouteBuilder MapProbeRuntimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/probes/{probeId}/runtime-state", async (
            string probeId,
            IProbeRepository probeRepository,
            IProbeTestConfigurationRepository probeTestConfigurationRepository,
            CancellationToken cancellationToken) =>
        {
            var normalizedProbeId = probeId.Trim();
            if (string.IsNullOrWhiteSpace(normalizedProbeId))
            {
                return Results.BadRequest(new { message = "Probe ID is required." });
            }

            var parsedProbeId = new ProbeId(normalizedProbeId);
            var probe = await probeRepository.GetByIdAsync(parsedProbeId, cancellationToken);
            if (probe is null)
            {
                return Results.NotFound(new { message = "Probe not found." });
            }

            var enabledTests = await probeTestConfigurationRepository.GetEnabledByProbeIdAsync(parsedProbeId, cancellationToken);
            var enabledTestTypes = enabledTests
                .Select(test => test.TestType.Name.ToUpperInvariant())
                .Distinct()
                .ToList();

            var canEmitMetrics = probe.Status == ProbeStatus.Active;

            return Results.Ok(new
            {
                probeId = probe.Id.Value,
                status = probe.Status.ToString().ToUpperInvariant(),
                canEmitMetrics,
                enabledTests = enabledTestTypes,
                site = probe.Location,
                ipAddress = probe.IpAddress,
                polledAtUtc = DateTimeOffset.UtcNow
            });
        })
            .WithName("GetProbeRuntimeState")
            .RequireAuthorization(AuthorizationPolicies.ProbeOrAdmin);

        endpoints.MapPost("/probes/{probeId}/heartbeat", async (
            string probeId,
            IProbeRepository probeRepository,
            CancellationToken cancellationToken) =>
        {
            var normalizedProbeId = probeId.Trim();
            if (string.IsNullOrWhiteSpace(normalizedProbeId))
            {
                return Results.BadRequest(new { message = "Probe ID is required." });
            }

            var parsedProbeId = new ProbeId(normalizedProbeId);
            var probe = await probeRepository.GetByIdAsync(parsedProbeId, cancellationToken);
            if (probe is null)
            {
                return Results.NotFound(new { message = "Probe not found." });
            }

            if (probe.Status == ProbeStatus.Decommissioned)
            {
                return Results.Conflict(new
                {
                    message = "Decommissioned probes cannot send heartbeat.",
                    probeId = probe.Id.Value,
                    status = probe.Status.ToString().ToUpperInvariant()
                });
            }

            probe.RecordHeartbeat();
            await probeRepository.UpdateAsync(probe, cancellationToken);

            return Results.Ok(new
            {
                probeId = probe.Id.Value,
                status = probe.Status.ToString().ToUpperInvariant(),
                lastHeartbeatUtc = probe.LastHeartbeat,
                receivedAtUtc = DateTimeOffset.UtcNow
            });
        })
            .WithName("RecordProbeHeartbeat")
            .RequireAuthorization(AuthorizationPolicies.ProbeOrAdmin);

        return endpoints;
    }
}
