namespace CentralServer.Tests.Support;

using CentralServer.Application.Abstractions;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}

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

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _plugins.Remove(id);
        return Task.CompletedTask;
    }
}

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

internal sealed class InMemoryProbeActionExecutionRepository : IProbeActionExecutionRepository
{
    private readonly Dictionary<string, ProbeActionExecution> _executions = new(StringComparer.OrdinalIgnoreCase);

    public Task<ProbeActionExecution> CreateAsync(ProbeActionExecution execution, CancellationToken cancellationToken = default)
    {
        _executions[execution.ExecutionId] = execution;
        return Task.FromResult(execution);
    }

    public Task<ProbeActionExecution?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default)
    {
        _executions.TryGetValue(executionId, out var execution);
        return Task.FromResult(execution);
    }

    public Task<IReadOnlyList<ProbeActionExecution>> ClaimPendingForProbeAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);
        var pending = _executions.Values
            .Where(e => string.Equals(e.ProbeId.Value, probeId.Value, StringComparison.OrdinalIgnoreCase)
                && e.Status == ProbeActionExecutionStatus.Queued)
            .OrderBy(e => e.RequestedAtUtc)
            .Take(safeLimit)
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var execution in pending)
        {
            execution.MarkDelivered(now);
        }

        return Task.FromResult<IReadOnlyList<ProbeActionExecution>>(pending);
    }

    public Task<IReadOnlyList<ProbeActionExecution>> GetByProbeIdAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var result = _executions.Values
            .Where(e => string.Equals(e.ProbeId.Value, probeId.Value, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.RequestedAtUtc)
            .Take(safeLimit)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProbeActionExecution>>(result);
    }

    public Task UpdateAsync(ProbeActionExecution execution, CancellationToken cancellationToken = default)
    {
        _executions[execution.ExecutionId] = execution;
        return Task.CompletedTask;
    }
}
