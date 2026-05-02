namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryProbePluginAssignmentRepository : IProbePluginAssignmentRepository
{
    private readonly Dictionary<string, Dictionary<string, ProbePluginAssignment>> _assignments =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ProbePluginAssignment>> GetByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default)
    {
        if (!_assignments.TryGetValue(probeId.Value, out var plugins))
        {
            return Task.FromResult<IReadOnlyList<ProbePluginAssignment>>([]);
        }

        return Task.FromResult<IReadOnlyList<ProbePluginAssignment>>(plugins.Values.OrderBy(a => a.PluginId).ToList());
    }

    public Task SetForProbeAsync(ProbeId probeId, IReadOnlyCollection<string> pluginIds, CancellationToken cancellationToken = default)
    {
        if (!_assignments.TryGetValue(probeId.Value, out var probeAssignments))
        {
            probeAssignments = new Dictionary<string, ProbePluginAssignment>(StringComparer.OrdinalIgnoreCase);
            _assignments[probeId.Value] = probeAssignments;
        }

        var normalized = pluginIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toRemove = probeAssignments.Keys.Where(key => !normalized.Contains(key)).ToList();
        foreach (var key in toRemove)
        {
            probeAssignments.Remove(key);
        }

        foreach (var pluginId in normalized)
        {
            if (probeAssignments.ContainsKey(pluginId))
            {
                continue;
            }

            probeAssignments[pluginId] = new ProbePluginAssignment(probeId, pluginId);
        }

        return Task.CompletedTask;
    }
}
