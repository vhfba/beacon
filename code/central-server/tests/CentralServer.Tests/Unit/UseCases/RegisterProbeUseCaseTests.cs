namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class RegisterProbeUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_DuplicateIp_ThrowsProbeRegistrationException()
    {
        var repo = new InMemoryProbeRepository();
        await repo.RegisterAsync(new Probe(new ProbeId("probe-existing"), "Existing", "Lab", "10.10.10.10"));
        var useCase = new RegisterProbeUseCase(repo);

        var input = new RegisterProbeInput
        {
            Id = "probe-new",
            Name = "Probe New",
            Location = "Lab",
            IpAddress = "10.10.10.10"
        };

        await Assert.ThrowsAsync<ProbeRegistrationException>(() => useCase.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsRegisteredProbeDto()
    {
        var repo = new InMemoryProbeRepository();
        var useCase = new RegisterProbeUseCase(repo);

        var input = new RegisterProbeInput
        {
            Id = "probe-100",
            Name = "Probe 100",
            Location = "Floor 1",
            IpAddress = "192.168.1.10"
        };

        var result = await useCase.ExecuteAsync(input);

        Assert.Equal("probe-100", result.Id);
        Assert.Equal("Probe 100", result.Name);
        Assert.Equal("Floor 1", result.Location);
        Assert.Equal("192.168.1.10", result.IpAddress);
        Assert.Equal(ProbeStatus.Registered.ToString(), result.Status);
    }
}
