namespace CentralServer.Tests.Unit.Domain;

using CentralServer.Domain.Models;

public class ProbeTestConfigurationTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(3600)]
    public void Constructor_ValidBoundaryIntervals_AreAccepted(int interval)
    {
        var probeId = new ProbeId("probe-09");

        var config = new ProbeTestConfiguration(probeId, "DNS", interval);

        Assert.Equal(interval, config.IntervalSeconds);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(3601)]
    public void Constructor_InvalidInterval_ThrowsDomainException(int interval)
    {
        var probeId = new ProbeId("probe-10");

        Assert.Throws<DomainException>(() => new ProbeTestConfiguration(probeId, "PING", interval));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidPluginId_ThrowsDomainException(string invalidPluginId)
    {
        var probeId = new ProbeId("probe-10");

        Assert.Throws<DomainException>(() => new ProbeTestConfiguration(probeId, invalidPluginId, 30));
    }

    [Fact]
    public void Constructor_PluginIdTooLong_ThrowsDomainException()
    {
        var probeId = new ProbeId("probe-10");
        var longPluginId = new string('X', 51);

        Assert.Throws<DomainException>(() => new ProbeTestConfiguration(probeId, longPluginId, 30));
    }

    [Fact]
    public void WithEnabled_ChangesOnlyEnabledFlag()
    {
        var probeId = new ProbeId("probe-11");
        var config = new ProbeTestConfiguration(probeId, "HTTP", 30, enabled: true);

        var updated = config.WithEnabled(false);

        Assert.False(updated.Enabled);
        Assert.Equal(config.IntervalSeconds, updated.IntervalSeconds);
        Assert.Equal(config.PluginId, updated.PluginId);
        Assert.Equal(config.ProbeId.Value, updated.ProbeId.Value);
    }

    [Fact]
    public void WithInterval_ChangesOnlyInterval()
    {
        var probeId = new ProbeId("probe-12");
        var config = new ProbeTestConfiguration(probeId, "PING", 20, enabled: false);

        var updated = config.WithInterval(45);

        Assert.Equal(45, updated.IntervalSeconds);
        Assert.False(updated.Enabled);
        Assert.Equal(config.PluginId, updated.PluginId);
        Assert.Equal(config.ProbeId.Value, updated.ProbeId.Value);
    }
}
