namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class UpdateProbeActionStatusUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRunningThenSucceeded_UpdatesStatus()
    {
        var repo = new InMemoryProbeActionExecutionRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var useCase = new UpdateProbeActionStatusUseCase(repo, unitOfWork);

        var execution = new ProbeActionExecution(new ProbeId("probe-1"), "action-wifi", "admin");
        await repo.CreateAsync(execution);
        await repo.ClaimPendingForProbeAsync(new ProbeId("probe-1"), 10);

        var running = await useCase.ExecuteAsync(new UpdateProbeActionStatusInput
        {
            ProbeId = "probe-1",
            ExecutionId = execution.ExecutionId,
            Status = ProbeActionExecutionStatus.Running
        });

        var succeeded = await useCase.ExecuteAsync(new UpdateProbeActionStatusInput
        {
            ProbeId = "probe-1",
            ExecutionId = execution.ExecutionId,
            Status = ProbeActionExecutionStatus.Succeeded
        });

        Assert.Equal(ProbeActionExecutionStatus.Running, running.Status);
        Assert.Equal(ProbeActionExecutionStatus.Succeeded, succeeded.Status);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProbeMismatch_ThrowsDomainException()
    {
        var repo = new InMemoryProbeActionExecutionRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var useCase = new UpdateProbeActionStatusUseCase(repo, unitOfWork);

        var execution = new ProbeActionExecution(new ProbeId("probe-1"), "action-wifi", "admin");
        await repo.CreateAsync(execution);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new UpdateProbeActionStatusInput
        {
            ProbeId = "probe-2",
            ExecutionId = execution.ExecutionId,
            Status = ProbeActionExecutionStatus.Running
        }));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
