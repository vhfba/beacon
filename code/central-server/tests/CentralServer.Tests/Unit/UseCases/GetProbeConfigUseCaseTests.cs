namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class GetProbeConfigUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_DoesNotMutateProbeState()
    {
        var probeRepo = new InMemoryProbeRepository();
        var configRepo = new InMemoryProbeTestConfigurationRepository();
        var pluginRepo = new InMemoryPluginRepository();
        var assignmentRepo = new InMemoryProbePluginAssignmentRepository();

        var probe = new Probe(new ProbeId("probe-config"), "Probe Config", "Lab", "10.0.0.7");
        await probeRepo.RegisterAsync(probe);
        await configRepo.UpdateAsync(new ProbeTestConfiguration(
            probe.Id,
            "PING",
            30,
            true));
        await pluginRepo.CreateAsync(new Plugin("plugin-a", "Plugin A", "1.0.0", "sha-a"));
        await assignmentRepo.SetForProbeAsync(probe.Id, ["plugin-a"]);

        var useCase = new GetProbeConfigUseCase(probeRepo, configRepo, pluginRepo, assignmentRepo);

        var result = await useCase.ExecuteAsync("probe-config");

        Assert.Equal("probe-config", result.ProbeId);
        Assert.Single(result.EnabledTests);
        Assert.Single(result.AvailablePlugins);

        var persisted = await probeRepo.GetByIdAsync(probe.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.LastConfigFetch);
    }

    [Fact]
    public async Task RecordProbeConfigFetch_UpdatesProbeAndSavesOnce()
    {
        var probeRepo = new InMemoryProbeRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var probe = new Probe(new ProbeId("probe-config"), "Probe Config", "Lab", "10.0.0.7");
        await probeRepo.RegisterAsync(probe);

        var useCase = new RecordProbeConfigFetchUseCase(probeRepo, unitOfWork);

        await useCase.ExecuteAsync("probe-config");

        var persisted = await probeRepo.GetByIdAsync(probe.Id);
        Assert.NotNull(persisted);
        Assert.NotNull(persisted!.LastConfigFetch);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
