namespace CentralServer.Application.UseCases;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal static class UseCaseGuards
{
    public static ProbeId CreateProbeId(string probeId, bool trim = false)
    {
        return new ProbeId(trim ? probeId.Trim() : probeId);
    }

    public static async Task<Probe> GetRequiredProbeAsync(
        IProbeRepository probeRepository,
        string probeId,
        CancellationToken cancellationToken,
        bool trim = false)
    {
        var parsedProbeId = CreateProbeId(probeId, trim);
        return await probeRepository.GetByIdAsync(parsedProbeId, cancellationToken)
            ?? throw new DomainException($"Probe {probeId} not found");
    }

    public static List<string> NormalizePluginIds(IEnumerable<string> pluginIds)
    {
        return pluginIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
