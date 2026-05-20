namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryProbeControlCommandRepository : IProbeControlCommandRepository
{
    private readonly Dictionary<string, ProbeControlCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public Task<ProbeControlCommand> CreateAsync(ProbeControlCommand command, CancellationToken cancellationToken = default)
    {
        _commands[command.CommandId] = command;
        return Task.FromResult(command);
    }

    public Task<ProbeControlCommand?> GetByIdAsync(string commandId, CancellationToken cancellationToken = default)
    {
        _commands.TryGetValue(commandId, out var command);
        return Task.FromResult(command);
    }

    public Task<IReadOnlyList<ProbeControlCommand>> ClaimPendingForProbeAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);
        var pending = _commands.Values
            .Where(c => string.Equals(c.ProbeId.Value, probeId.Value, StringComparison.OrdinalIgnoreCase)
                && c.Status == ProbeControlCommandStatus.Queued)
            .OrderBy(c => c.RequestedAtUtc)
            .Take(safeLimit)
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var command in pending)
        {
            command.MarkDelivered(now);
        }

        return Task.FromResult<IReadOnlyList<ProbeControlCommand>>(pending);
    }

    public Task<IReadOnlyList<ProbeControlCommand>> GetByProbeIdAsync(
        ProbeId probeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var result = _commands.Values
            .Where(c => string.Equals(c.ProbeId.Value, probeId.Value, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.RequestedAtUtc)
            .Take(safeLimit)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProbeControlCommand>>(result);
    }

    public Task UpdateAsync(ProbeControlCommand command, CancellationToken cancellationToken = default)
    {
        _commands[command.CommandId] = command;
        return Task.CompletedTask;
    }
}
