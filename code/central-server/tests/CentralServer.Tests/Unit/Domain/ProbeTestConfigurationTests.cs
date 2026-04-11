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
        var testType = new TestType("DNS", "DNS resolve test");

        var config = new ProbeTestConfiguration(probeId, testType, interval);

        Assert.Equal(interval, config.IntervalSeconds);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(3601)]
    public void Constructor_InvalidInterval_ThrowsDomainException(int interval)
    {
        var probeId = new ProbeId("probe-10");
        var testType = new TestType("PING", "ICMP latency test");

        Assert.Throws<DomainException>(() => new ProbeTestConfiguration(probeId, testType, interval));
    }

    [Fact]
    public void WithEnabled_ChangesOnlyEnabledFlag()
    {
        var probeId = new ProbeId("probe-11");
        var testType = new TestType("HTTP", "HTTP health check");
        var config = new ProbeTestConfiguration(probeId, testType, 30, enabled: true);

        var updated = config.WithEnabled(false);

        Assert.False(updated.Enabled);
        Assert.Equal(config.IntervalSeconds, updated.IntervalSeconds);
        Assert.Equal(config.TestType.Name, updated.TestType.Name);
        Assert.Equal(config.ProbeId.Value, updated.ProbeId.Value);
    }

    [Fact]
    public void WithInterval_ChangesOnlyInterval()
    {
        var probeId = new ProbeId("probe-12");
        var testType = new TestType("PING", "ICMP health check");
        var config = new ProbeTestConfiguration(probeId, testType, 20, enabled: false);

        var updated = config.WithInterval(45);

        Assert.Equal(45, updated.IntervalSeconds);
        Assert.False(updated.Enabled);
        Assert.Equal(config.TestType.Name, updated.TestType.Name);
        Assert.Equal(config.ProbeId.Value, updated.ProbeId.Value);
    }
}
