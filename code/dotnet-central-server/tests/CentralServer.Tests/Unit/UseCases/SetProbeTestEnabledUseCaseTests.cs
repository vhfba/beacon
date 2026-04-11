namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class SetProbeTestEnabledUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ProbeNotFound_ThrowsDomainException()
    {
        var probeRepo = new InMemoryProbeRepository();
        var configRepo = new InMemoryProbeTestConfigurationRepository();
        var useCase = new SetProbeTestEnabledUseCase(probeRepo, configRepo);

        var input = new SetProbeTestEnabledInput
        {
            ProbeId = "missing-probe",
            TestType = "PING",
            Enabled = false
        };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_ExistingConfig_UpdatesEnabledFlag()
    {
        var probeRepo = new InMemoryProbeRepository();
        var configRepo = new InMemoryProbeTestConfigurationRepository();
        var probe = new Probe(new ProbeId("probe-200"), "Probe 200", "HQ", "10.0.0.200");
        var testType = new TestType("PING", "ICMP latency");

        await probeRepo.RegisterAsync(probe);
        await configRepo.UpdateAsync(new ProbeTestConfiguration(probe.Id, testType, 30, enabled: true));

        var useCase = new SetProbeTestEnabledUseCase(probeRepo, configRepo);
        var input = new SetProbeTestEnabledInput
        {
            ProbeId = "probe-200",
            TestType = "PING",
            Enabled = false
        };

        var result = await useCase.ExecuteAsync(input);

        Assert.False(result.Enabled);
        var stored = await configRepo.GetAsync(new ProbeId("probe-200"), "PING");
        Assert.NotNull(stored);
        Assert.False(stored!.Enabled);
    }
}
