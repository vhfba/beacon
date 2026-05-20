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
        var unitOfWork = new NoOpUnitOfWork();
        var useCase = new SetProbeTestEnabledUseCase(probeRepo, configRepo, unitOfWork);

        var input = new SetProbeTestEnabledInput
        {
            ProbeId = "missing-probe",
            TestType = "PING",
            Enabled = false
        };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(input));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingConfig_UpdatesEnabledFlag()
    {
        var probeRepo = new InMemoryProbeRepository();
        var configRepo = new InMemoryProbeTestConfigurationRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var probe = new Probe(new ProbeId("probe-200"), "Probe 200", "HQ", "10.0.0.200");
        await probeRepo.RegisterAsync(probe);
        await configRepo.UpdateAsync(new ProbeTestConfiguration(probe.Id, "PING", 30, enabled: true));

        var useCase = new SetProbeTestEnabledUseCase(probeRepo, configRepo, unitOfWork);
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
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
