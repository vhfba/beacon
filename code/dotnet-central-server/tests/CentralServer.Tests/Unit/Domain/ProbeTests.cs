namespace CentralServer.Tests.Unit.Domain;

using CentralServer.Domain.Models;

public class ProbeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsDomainException(string invalidName)
    {
        Assert.Throws<DomainException>(() => new Probe(new ProbeId("probe-invalid-name"), invalidName, "HQ", "10.0.0.1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidLocation_ThrowsDomainException(string invalidLocation)
    {
        Assert.Throws<DomainException>(() => new Probe(new ProbeId("probe-invalid-location"), "Probe", invalidLocation, "10.0.0.1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidIpAddress_ThrowsDomainException(string invalidIpAddress)
    {
        Assert.Throws<DomainException>(() => new Probe(new ProbeId("probe-invalid-ip"), "Probe", "HQ", invalidIpAddress));
    }

    [Fact]
    public void Constructor_ValidData_InitializesAsRegistered()
    {
        var probe = new Probe(new ProbeId("probe-01"), "Probe 01", "Building A", "10.0.0.1");

        Assert.Equal("probe-01", probe.Id.Value);
        Assert.Equal(ProbeStatus.Registered, probe.Status);
        Assert.Null(probe.LastHeartbeat);
        Assert.Null(probe.LastConfigFetch);
        Assert.Equal(0, probe.Version);
    }

    [Fact]
    public void RecordHeartbeat_FromRegistered_SetsActiveAndHeartbeat()
    {
        var probe = new Probe(new ProbeId("probe-02"), "Probe 02", "Building B", "10.0.0.2");

        probe.RecordHeartbeat();

        Assert.Equal(ProbeStatus.Active, probe.Status);
        Assert.NotNull(probe.LastHeartbeat);
        Assert.Equal(1, probe.Version);
    }

    [Fact]
    public void RecordHeartbeat_FromDecommissioned_KeepsDecommissioned()
    {
        var probe = new Probe(new ProbeId("probe-03"), "Probe 03", "Building C", "10.0.0.3");
        probe.UpdateStatus(ProbeStatus.Decommissioned);
        var versionAfterDecommission = probe.Version;

        probe.RecordHeartbeat();

        Assert.Equal(ProbeStatus.Decommissioned, probe.Status);
        Assert.NotNull(probe.LastHeartbeat);
        Assert.Equal(versionAfterDecommission, probe.Version);
    }

    [Fact]
    public void UpdateStatus_IncrementsVersion()
    {
        var probe = new Probe(new ProbeId("probe-05"), "Probe 05", "Building E", "10.0.0.5");

        probe.UpdateStatus(ProbeStatus.Active);
        probe.UpdateStatus(ProbeStatus.Inactive);

        Assert.Equal(2, probe.Version);
        Assert.Equal(ProbeStatus.Inactive, probe.Status);
    }

    [Fact]
    public void RecordConfigFetch_SetsTimestampWithoutChangingVersion()
    {
        var probe = new Probe(new ProbeId("probe-06"), "Probe 06", "Building F", "10.0.0.6");
        var previousVersion = probe.Version;

        probe.RecordConfigFetch();

        Assert.NotNull(probe.LastConfigFetch);
        Assert.Equal(previousVersion, probe.Version);
    }

    [Fact]
    public void Rehydrate_InvalidStatus_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Probe.Rehydrate(
            id: "probe-04",
            name: "Probe 04",
            location: "Building D",
            ipAddress: "10.0.0.4",
            status: "NOT_A_STATUS",
            createdAt: DateTime.UtcNow,
            lastHeartbeat: null,
            lastConfigFetch: null,
            version: 1));
    }

    [Fact]
    public void Rehydrate_ValidValues_PreservesPersistedState()
    {
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var lastHeartbeat = DateTime.UtcNow.AddMinutes(-5);
        var lastConfigFetch = DateTime.UtcNow.AddMinutes(-1);

        var probe = Probe.Rehydrate(
            id: "probe-07",
            name: "Probe 07",
            location: "Building G",
            ipAddress: "10.0.0.7",
            status: ProbeStatus.Inactive.ToString(),
            createdAt: createdAt,
            lastHeartbeat: lastHeartbeat,
            lastConfigFetch: lastConfigFetch,
            version: 42);

        Assert.Equal("probe-07", probe.Id.Value);
        Assert.Equal(ProbeStatus.Inactive, probe.Status);
        Assert.Equal(createdAt, probe.CreatedAt);
        Assert.Equal(lastHeartbeat, probe.LastHeartbeat);
        Assert.Equal(lastConfigFetch, probe.LastConfigFetch);
        Assert.Equal(42, probe.Version);
    }
}
