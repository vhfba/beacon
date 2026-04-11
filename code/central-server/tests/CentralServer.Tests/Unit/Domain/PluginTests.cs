namespace CentralServer.Tests.Unit.Domain;

using CentralServer.Domain.Models;

public class PluginTests
{
    [Theory]
    [InlineData("", "name", "1.0.0", "checksum")]
    [InlineData("id", "", "1.0.0", "checksum")]
    [InlineData("id", "name", "", "checksum")]
    [InlineData("id", "name", "1.0.0", "")]
    public void Constructor_RequiredFieldsMissing_ThrowsDomainException(
        string id,
        string name,
        string version,
        string checksum)
    {
        Assert.Throws<DomainException>(() => new Plugin(id, name, version, checksum));
    }

    [Fact]
    public void Constructor_InvalidLengthConstraints_ThrowDomainException()
    {
        var longId = new string('a', 101);
        var longName = new string('b', 101);
        var longVersion = new string('c', 51);
        var longChecksum = new string('d', 129);

        Assert.Throws<DomainException>(() => new Plugin(longId, "name", "1.0.0", "checksum"));
        Assert.Throws<DomainException>(() => new Plugin("id", longName, "1.0.0", "checksum"));
        Assert.Throws<DomainException>(() => new Plugin("id", "name", longVersion, "checksum"));
        Assert.Throws<DomainException>(() => new Plugin("id", "name", "1.0.0", longChecksum));
    }

    [Fact]
    public void RetireAndRestore_ToggleAvailability()
    {
        var plugin = new Plugin("plugin-dns", "DNS Plugin", "2.1.0", "sha256");

        plugin.Retire();
        Assert.False(plugin.Available);

        plugin.Restore();
        Assert.True(plugin.Available);
    }

    [Fact]
    public void Rehydrate_PreservesPersistedState()
    {
        var releasedAt = DateTime.UtcNow.AddDays(-10);

        var plugin = Plugin.Rehydrate(
            id: "plugin-http",
            name: "HTTP",
            version: "1.2.3",
            checksum: "abc123",
            description: "HTTP checks",
            releasedAt: releasedAt,
            available: false);

        Assert.Equal("plugin-http", plugin.Id);
        Assert.Equal("HTTP", plugin.Name);
        Assert.Equal("1.2.3", plugin.Version);
        Assert.Equal("abc123", plugin.Checksum);
        Assert.Equal("HTTP checks", plugin.Description);
        Assert.Equal(releasedAt, plugin.ReleasedAt);
        Assert.False(plugin.Available);
    }
}
