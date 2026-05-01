namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class UpdateProbeStatusUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_InvalidStatus_ThrowsDomainException()
    {
        var probeRepo = new InMemoryProbeRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-300"), "Probe 300", "HQ", "10.0.0.30"));
        var useCase = new UpdateProbeStatusUseCase(probeRepo, unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync("probe-300", "broken-status"));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ValidStatus_UpdatesProbe()
    {
        var probeRepo = new InMemoryProbeRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-301"), "Probe 301", "HQ", "10.0.0.31"));
        var useCase = new UpdateProbeStatusUseCase(probeRepo, unitOfWork);

        var result = await useCase.ExecuteAsync("probe-301", ProbeStatus.Inactive.ToString());

        Assert.Equal(ProbeStatus.Inactive.ToString(), result.Status);

        var persisted = await probeRepo.GetByIdAsync(new ProbeId("probe-301"));
        Assert.NotNull(persisted);
        Assert.Equal(ProbeStatus.Inactive, persisted!.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
