namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryProbeTestConfigurationRepository : IProbeTestConfigurationRepository
{
    private readonly Dictionary<(string ProbeId, string TestType), ProbeTestConfiguration> _configs = new();

    public Task<IReadOnlyList<ProbeTestConfiguration>> GetByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default)
    {
        var values = _configs
            .Where(kvp => string.Equals(kvp.Key.ProbeId, probeId.Value, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProbeTestConfiguration>>(values);
    }

    public Task<IReadOnlyList<ProbeTestConfiguration>> GetEnabledByProbeIdAsync(ProbeId probeId, CancellationToken cancellationToken = default)
    {
        var values = _configs
            .Where(kvp => string.Equals(kvp.Key.ProbeId, probeId.Value, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)
            .Where(c => c.Enabled)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProbeTestConfiguration>>(values);
    }

    public Task<ProbeTestConfiguration?> GetAsync(ProbeId probeId, string testTypeName, CancellationToken cancellationToken = default)
    {
        _configs.TryGetValue((probeId.Value, testTypeName), out var config);
        return Task.FromResult(config);
    }

    public Task UpdateAsync(ProbeTestConfiguration config, CancellationToken cancellationToken = default)
    {
        _configs[(config.ProbeId.Value, config.TestType.Name)] = config;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProbeId probeId, string testTypeName, CancellationToken cancellationToken = default)
    {
        _configs.Remove((probeId.Value, testTypeName));
        return Task.CompletedTask;
    }
}
