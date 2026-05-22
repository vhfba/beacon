namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class UpdatePluginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesCatalogDetails()
    {
        var pluginRepo = new InMemoryPluginRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await pluginRepo.CreateAsync(new Plugin("wifi-old", "Wi-Fi Old", "1.0.0", "old-sha"));
        var useCase = new UpdatePluginUseCase(pluginRepo, unitOfWork);

        var result = await useCase.ExecuteAsync(new UpdatePluginInput
        {
            CurrentId = "wifi-old",
            Id = "wifi-new",
            Name = "Wi-Fi New",
            Version = "1.0.1",
            Checksum = "new-sha",
            Description = "Updated analyzer",
            DashboardJson = "{\"title\":\"Wi-Fi\"}",
            ExecutionMode = PluginExecutionMode.Scheduled
        });

        Assert.Equal("wifi-new", result.Id);
        Assert.Equal("Wi-Fi New", result.Name);
        Assert.Equal("1.0.1", result.Version);
        Assert.Equal("new-sha", result.Checksum);
        Assert.Null(await pluginRepo.GetByIdAsync("wifi-old"));
        Assert.NotNull(await pluginRepo.GetByIdAsync("wifi-new"));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateId_ThrowsDomainException()
    {
        var pluginRepo = new InMemoryPluginRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await pluginRepo.CreateAsync(new Plugin("wifi-a", "Wi-Fi A", "1.0.0", "sha-a"));
        await pluginRepo.CreateAsync(new Plugin("wifi-b", "Wi-Fi B", "1.0.0", "sha-b"));
        var useCase = new UpdatePluginUseCase(pluginRepo, unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new UpdatePluginInput
        {
            CurrentId = "wifi-a",
            Id = "wifi-b",
            Name = "Wi-Fi A",
            Version = "1.0.1",
            Checksum = "sha-c",
            ExecutionMode = PluginExecutionMode.Scheduled
        }));
    }
}
