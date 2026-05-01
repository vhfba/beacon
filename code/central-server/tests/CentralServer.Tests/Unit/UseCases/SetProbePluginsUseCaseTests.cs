namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class SetProbePluginsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AssignsSelectedPlugins()
    {
        var probeRepo = new InMemoryProbeRepository();
        var pluginRepo = new InMemoryPluginRepository();
        var assignmentRepo = new InMemoryProbePluginAssignmentRepository();
        var unitOfWork = new NoOpUnitOfWork();

        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));
        await pluginRepo.CreateAsync(new Plugin("plugin-a", "Plugin A", "1.0.0", "sha-a"));
        await pluginRepo.CreateAsync(new Plugin("plugin-b", "Plugin B", "1.0.0", "sha-b"));

        var useCase = new SetProbePluginsUseCase(probeRepo, pluginRepo, assignmentRepo, unitOfWork);

        var result = await useCase.ExecuteAsync(new SetProbePluginsInput
        {
            ProbeId = "probe-1",
            PluginIds = ["plugin-a", "plugin-b"]
        });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.PluginId == "plugin-a");
        Assert.Contains(result, item => item.PluginId == "plugin-b");
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPluginMissing_ThrowsDomainException()
    {
        var probeRepo = new InMemoryProbeRepository();
        var pluginRepo = new InMemoryPluginRepository();
        var assignmentRepo = new InMemoryProbePluginAssignmentRepository();
        var unitOfWork = new NoOpUnitOfWork();

        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));

        var useCase = new SetProbePluginsUseCase(probeRepo, pluginRepo, assignmentRepo, unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new SetProbePluginsInput
        {
            ProbeId = "probe-1",
            PluginIds = ["missing-plugin"]
        }));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
