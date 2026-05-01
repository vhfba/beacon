namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class ProbeActionRetrievalUseCaseTests
{
    [Fact]
    public async Task GetPendingProbeActions_WhenProbeExists_ClaimsQueuedActionsOnly()
    {
        var probeRepo = new InMemoryProbeRepository();
        var executionRepo = new InMemoryProbeActionExecutionRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));

        var queued = await executionRepo.CreateAsync(new ProbeActionExecution(new ProbeId("probe-1"), "action-one", "admin"));
        var delivered = await executionRepo.CreateAsync(new ProbeActionExecution(new ProbeId("probe-1"), "action-two", "admin"));
        delivered.MarkDelivered(DateTime.UtcNow);

        var useCase = new GetPendingProbeActionsUseCase(probeRepo, executionRepo, unitOfWork);

        var result = await useCase.ExecuteAsync("probe-1", 10);

        Assert.Single(result);
        Assert.Equal(queued.ExecutionId, result[0].ExecutionId);
        Assert.Equal(ProbeActionExecutionStatus.Delivered, result[0].Status);
        Assert.NotNull(result[0].DeliveredAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ListProbeActionExecutions_WhenProbeExists_ReturnsMostRecentFirst()
    {
        var probeRepo = new InMemoryProbeRepository();
        var executionRepo = new InMemoryProbeActionExecutionRepository();
        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));

        var older = ProbeActionExecution.Rehydrate(
            executionId: "older",
            probeId: new ProbeId("probe-1"),
            pluginId: "action-old",
            triggeredBy: "admin",
            status: ProbeActionExecutionStatus.Succeeded,
            requestedAtUtc: DateTime.UtcNow.AddMinutes(-10),
            deliveredAtUtc: DateTime.UtcNow.AddMinutes(-9),
            startedAtUtc: DateTime.UtcNow.AddMinutes(-8),
            completedAtUtc: DateTime.UtcNow.AddMinutes(-7),
            errorMessage: null);
        var newer = ProbeActionExecution.Rehydrate(
            executionId: "newer",
            probeId: new ProbeId("probe-1"),
            pluginId: "action-new",
            triggeredBy: "admin",
            status: ProbeActionExecutionStatus.Queued,
            requestedAtUtc: DateTime.UtcNow.AddMinutes(-1),
            deliveredAtUtc: null,
            startedAtUtc: null,
            completedAtUtc: null,
            errorMessage: null);

        await executionRepo.CreateAsync(older);
        await executionRepo.CreateAsync(newer);

        var useCase = new ListProbeActionExecutionsUseCase(probeRepo, executionRepo);

        var result = await useCase.ExecuteAsync("probe-1", 10);

        Assert.Collection(result,
            first => Assert.Equal("newer", first.ExecutionId),
            second => Assert.Equal("older", second.ExecutionId));
    }

    [Fact]
    public async Task GetPendingProbeActions_WhenProbeMissing_ThrowsDomainException()
    {
        var unitOfWork = new NoOpUnitOfWork();
        var useCase = new GetPendingProbeActionsUseCase(
            new InMemoryProbeRepository(),
            new InMemoryProbeActionExecutionRepository(),
            unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync("missing-probe", 10));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
