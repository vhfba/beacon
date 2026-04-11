namespace CentralServer.Tests.Unit.Domain;

using CentralServer.Domain.Models;

public class ProbeIdTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespace_ThrowsDomainException(string invalidValue)
    {
        Assert.Throws<DomainException>(() => new ProbeId(invalidValue));
    }

    [Fact]
    public void Constructor_ValueLongerThan50_ThrowsDomainException()
    {
        var tooLong = new string('a', 51);

        Assert.Throws<DomainException>(() => new ProbeId(tooLong));
    }

    [Fact]
    public void ToString_ReturnsOriginalValue()
    {
        var probeId = new ProbeId("probe-xyz");

        Assert.Equal("probe-xyz", probeId.ToString());
    }
}
