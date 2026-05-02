namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

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
