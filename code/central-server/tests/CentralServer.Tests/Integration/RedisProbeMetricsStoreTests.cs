using CentralServer.Application.DTOs;
using CentralServer.Infrastructure.Metrics;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace CentralServer.Tests.Integration;

public class RedisProbeMetricsStoreTests : IAsyncLifetime
{
    private RedisContainer _redisContainer = null!;
    private ConnectionMultiplexer _redisConnection = null!;
    private RedisProbeMetricsStore _sut = null!;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder().WithImage("redis:7-alpine").Build();
        await _redisContainer.StartAsync();

        _redisConnection = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        var options = Options.Create(new MetricsStoreOptions
        {
            Redis = new RedisMetricsStoreOptions
            {
                ProbeSnapshotTtlSeconds = 1
            }
        });

        _sut = new RedisProbeMetricsStore(_redisConnection, options);
    }

    public async Task DisposeAsync()
    {
        await _redisConnection.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task Store_And_Get_ReturnsMetrics()
    {
        var probeId = "probe-1";
        var receivedAt = DateTimeOffset.UtcNow;
        var samples = new List<MetricSampleInput>
        {
            new() { Name = "cpu_usage", Value = 50.0, Labels = new Dictionary<string, string>() }
        };

        await _sut.StoreProbeMetricsAsync(probeId, samples, receivedAt);

        var retrieved = await _sut.GetAllAsync();

        Assert.Single(retrieved);
        Assert.Equal(probeId, retrieved[0].ProbeId);
        Assert.Equal(50.0, retrieved[0].Samples[0].Value);
    }

    [Fact]
    public async Task Expired_Key_Is_Removed_Gracefully()
    {
        var probeId = "probe-expired";
        var receivedAt = DateTimeOffset.UtcNow;
        var samples = new List<MetricSampleInput>
        {
            new() { Name = "mem_usage", Value = 250.0, Labels = new Dictionary<string, string>() }
        };

        await _sut.StoreProbeMetricsAsync(probeId, samples, receivedAt);
        
        // Wait for TTL (1 second TTL, let's wait 2 seconds)
        await Task.Delay(2000);

        var retrieved = await _sut.GetAllAsync();

        Assert.Empty(retrieved);
        
        // Ensure Set was cleaned up gracefully
        var setMembers = await _redisConnection.GetDatabase().SetMembersAsync("beacon:metrics:probes");
        Assert.Empty(setMembers);
    }
}
