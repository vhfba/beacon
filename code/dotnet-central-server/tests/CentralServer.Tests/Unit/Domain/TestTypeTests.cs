namespace CentralServer.Tests.Unit.Domain;

using CentralServer.Domain.Models;

public class TestTypeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsDomainException(string invalidName)
    {
        Assert.Throws<DomainException>(() => new TestType(invalidName, "desc"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidDescription_ThrowsDomainException(string invalidDescription)
    {
        Assert.Throws<DomainException>(() => new TestType("PING", invalidDescription));
    }

    [Fact]
    public void Constructor_NameTooLong_ThrowsDomainException()
    {
        var longName = new string('X', 51);

        Assert.Throws<DomainException>(() => new TestType(longName, "desc"));
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var testType = new TestType("HTTP", "HTTP check");

        Assert.Equal("HTTP", testType.ToString());
    }
}
