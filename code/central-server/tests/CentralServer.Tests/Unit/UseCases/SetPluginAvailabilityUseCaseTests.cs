namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class SetPluginAvailabilityUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AvailableFalse_RetiresPlugin()
    {
        var pluginRepo = new InMemoryPluginRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await pluginRepo.CreateAsync(new Plugin("plugin-http", "HTTP Plugin", "1.0.0", "sha256"));
        var useCase = new SetPluginAvailabilityUseCase(pluginRepo, unitOfWork);

        var result = await useCase.ExecuteAsync(new SetPluginAvailabilityInput
        {
            PluginId = "plugin-http",
            Available = false
        });

        Assert.False(result.Available);

        var persisted = await pluginRepo.GetByIdAsync("plugin-http");
        Assert.NotNull(persisted);
        Assert.False(persisted!.Available);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPlugin_ThrowsDomainException()
    {
        var pluginRepo = new InMemoryPluginRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var useCase = new SetPluginAvailabilityUseCase(pluginRepo, unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new SetPluginAvailabilityInput
        {
            PluginId = "missing-plugin",
            Available = true
        }));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
