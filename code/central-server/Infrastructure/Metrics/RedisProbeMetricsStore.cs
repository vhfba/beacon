namespace CentralServer.Infrastructure.Metrics;

using System.Text.Json;
using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

public sealed class RedisProbeMetricsStore : IProbeMetricsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisMetricsStoreOptions _options;

    public RedisProbeMetricsStore(IConnectionMultiplexer redis, IOptions<MetricsStoreOptions> options)
    {
        _redis = redis;
        _options = options.Value.Redis;
    }

    public async Task StoreProbeMetricsAsync(string probeId, IReadOnlyList<MetricSampleInput> samples, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var snapshot = new ProbeMetricsSnapshot(probeId, receivedAtUtc, samples);
        var key = ProbeKey(probeId);
        var payload = JsonSerializer.Serialize(snapshot, SerializerOptions);

        await db.StringSetAsync(key, payload, TimeSpan.FromSeconds(Math.Max(30, _options.ProbeSnapshotTtlSeconds))).ConfigureAwait(false);
        await db.SetAddAsync(ProbesKey(), probeId).ConfigureAwait(false);
    }

    private const string RemoveFromSetIfStringMissingScript = @"
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return redis.call('SREM', KEYS[2], ARGV[1])
        else
            return 0
        end";

    public async Task<IReadOnlyList<ProbeMetricsSnapshot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var probeIds = await db.SetMembersAsync(ProbesKey()).ConfigureAwait(false);
        if (probeIds.Length == 0)
        {
            return [];
        }

        var snapshots = new List<ProbeMetricsSnapshot>(probeIds.Length);
        foreach (var probeIdValue in probeIds)
        {
            var probeId = probeIdValue.ToString();
            if (string.IsNullOrWhiteSpace(probeId))
            {
                continue;
            }

            var key = ProbeKey(probeId);
            var raw = await db.StringGetAsync(key).ConfigureAwait(false);
            if (raw.IsNullOrEmpty)
            {
                await db.ScriptEvaluateAsync(
                    RemoveFromSetIfStringMissingScript,
                    new RedisKey[] { key, ProbesKey() },
                    new RedisValue[] { probeId }
                ).ConfigureAwait(false);
                continue;
            }

            var snapshot = JsonSerializer.Deserialize<ProbeMetricsSnapshot>(raw!, SerializerOptions);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    private string ProbesKey() => $"{_options.KeyPrefix}:probes";

    private string ProbeKey(string probeId) => $"{_options.KeyPrefix}:probe:{probeId}";
}
