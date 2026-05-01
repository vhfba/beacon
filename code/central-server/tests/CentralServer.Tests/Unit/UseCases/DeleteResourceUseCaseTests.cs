namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class DeleteResourceUseCaseTests
{
    [Fact]
    public async Task DeleteProbe_RemovesProbe()
    {
        var probeRepo = new InMemoryProbeRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));

        var useCase = new DeleteProbeUseCase(probeRepo, unitOfWork);
        await useCase.ExecuteAsync("probe-1");

        var deleted = await probeRepo.GetByIdAsync(new ProbeId("probe-1"));
        Assert.Null(deleted);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeletePlugin_RemovesPlugin()
    {
        var pluginRepo = new InMemoryPluginRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await pluginRepo.CreateAsync(new Plugin("plugin-a", "Plugin A", "1.0.0", "sha-a"));

        var useCase = new DeletePluginUseCase(pluginRepo, unitOfWork);
        await useCase.ExecuteAsync("plugin-a");

        var deleted = await pluginRepo.GetByIdAsync("plugin-a");
        Assert.Null(deleted);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
