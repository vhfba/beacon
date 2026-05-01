namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;

public interface IProbePluginAssignmentRepository
{
    Task<IReadOnlyList<ProbePluginAssignment>> GetByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default);

    Task SetForProbeAsync(ProbeId probeId, IReadOnlyCollection<string> pluginIds, CancellationToken cancellationToken = default);
}
