namespace CentralServer.Tests.Unit.Domain;

using CentralServer.Domain.Models;

public class ProbeActionExecutionTests
{
    [Fact]
    public void Constructor_WhenValid_SetsQueuedStatus()
    {
        var execution = new ProbeActionExecution(new ProbeId("probe-1"), "action-wifi-scan", "admin");

        Assert.Equal(ProbeActionExecutionStatus.Queued, execution.Status);
        Assert.NotEmpty(execution.ExecutionId);
        Assert.Null(execution.StartedAtUtc);
        Assert.Null(execution.CompletedAtUtc);
    }

    [Fact]
    public void MarkDeliveredThenRunningThenSucceeded_CompletesLifecycle()
    {
        var execution = new ProbeActionExecution(new ProbeId("probe-1"), "action-wifi-scan", "admin");

        execution.MarkDelivered(DateTime.UtcNow);
        execution.MarkRunning(DateTime.UtcNow);
        execution.MarkSucceeded(DateTime.UtcNow);

        Assert.Equal(ProbeActionExecutionStatus.Succeeded, execution.Status);
        Assert.NotNull(execution.DeliveredAtUtc);
        Assert.NotNull(execution.StartedAtUtc);
        Assert.NotNull(execution.CompletedAtUtc);
    }

    [Fact]
    public void MarkSucceededWithoutRunning_ThrowsDomainException()
    {
        var execution = new ProbeActionExecution(new ProbeId("probe-1"), "action-wifi-scan", "admin");

        Assert.Throws<DomainException>(() => execution.MarkSucceeded(DateTime.UtcNow));
    }
}