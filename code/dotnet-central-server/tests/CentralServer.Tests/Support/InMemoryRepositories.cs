namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryProbeRepository : IProbeRepository
{
    private readonly Dictionary<string, Probe> _probes = new(StringComparer.OrdinalIgnoreCase);

    public Task<Probe> RegisterAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        _probes[probe.Id.Value] = probe;
        return Task.FromResult(probe);
    }

    public Task<Probe?> GetByIdAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        _probes.TryGetValue(id.Value, out var probe);
        return Task.FromResult(probe);
    }

    public Task<IReadOnlyList<Probe>> GetAllAsync(ProbeStatus? status = null, CancellationToken cancellationToken = default)
    {
        var values = status is null
            ? _probes.Values.ToList()
            : _probes.Values.Where(p => p.Status == status.Value).ToList();

        return Task.FromResult<IReadOnlyList<Probe>>(values);
    }

    public Task<Probe?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var probe = _probes.Values.FirstOrDefault(p => string.Equals(p.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(probe);
    }

    public Task UpdateAsync(Probe probe, CancellationToken cancellationToken = default)
    {
        _probes[probe.Id.Value] = probe;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProbeId id, CancellationToken cancellationToken = default)
    {
        _probes.Remove(id.Value);
        return Task.CompletedTask;
    }
}

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

internal sealed class InMemoryTestTypeRepository : ITestTypeRepository
{
    private readonly Dictionary<string, TestType> _types = new(StringComparer.OrdinalIgnoreCase);

    public Task<TestType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _types.TryGetValue(name, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<TestType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TestType>>(_types.Values.ToList());
    }

    public Task<TestType> CreateAsync(TestType testType, CancellationToken cancellationToken = default)
    {
        _types[testType.Name] = testType;
        return Task.FromResult(testType);
    }
}

internal sealed class InMemoryPluginRepository : IPluginRepository
{
    private readonly Dictionary<string, Plugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public Task<Plugin?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _plugins.TryGetValue(id, out var plugin);
        return Task.FromResult(plugin);
    }

    public Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plugin>>(_plugins.Values.ToList());
    }

    public Task<IReadOnlyList<Plugin>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plugin>>(_plugins.Values.Where(p => p.Available).ToList());
    }

    public Task<IReadOnlyList<Plugin>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plugin>>(
            _plugins.Values.Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public Task<Plugin?> GetLatestByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var latest = _plugins.Values
            .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.ReleasedAt)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<Plugin> CreateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        _plugins[plugin.Id] = plugin;
        return Task.FromResult(plugin);
    }

    public Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        _plugins[plugin.Id] = plugin;
        return Task.CompletedTask;
    }
}
