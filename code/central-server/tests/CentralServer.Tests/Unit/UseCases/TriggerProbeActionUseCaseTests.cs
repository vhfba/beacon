namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class TriggerProbeActionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenValid_QueuesActionExecution()
    {
        var probeRepo = new InMemoryProbeRepository();
        var pluginRepo = new InMemoryPluginRepository();
        var assignmentRepo = new InMemoryProbePluginAssignmentRepository();
        var executionRepo = new InMemoryProbeActionExecutionRepository();
        var unitOfWork = new NoOpUnitOfWork();

        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));
        await pluginRepo.CreateAsync(new Plugin("action-wifi", "WiFi Action", "1.0.0", "sha", executionMode: PluginExecutionMode.Action));
        await assignmentRepo.SetForProbeAsync(new ProbeId("probe-1"), ["action-wifi"]);

        var useCase = new TriggerProbeActionUseCase(probeRepo, pluginRepo, assignmentRepo, executionRepo, unitOfWork);
        var result = await useCase.ExecuteAsync(new TriggerProbeActionInput
        {
            ProbeId = "probe-1",
            PluginId = "action-wifi",
            TriggeredBy = "admin"
        });

        Assert.Equal("probe-1", result.ProbeId);
        Assert.Equal("action-wifi", result.PluginId);
        Assert.Equal(ProbeActionExecutionStatus.Queued, result.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPluginIsNotAction_ThrowsDomainException()
    {
        var probeRepo = new InMemoryProbeRepository();
        var pluginRepo = new InMemoryPluginRepository();
        var assignmentRepo = new InMemoryProbePluginAssignmentRepository();
        var executionRepo = new InMemoryProbeActionExecutionRepository();
        var unitOfWork = new NoOpUnitOfWork();

        await probeRepo.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe 1", "Lab", "10.0.0.1"));
        await pluginRepo.CreateAsync(new Plugin("plugin-http", "HTTP", "1.0.0", "sha", executionMode: PluginExecutionMode.Scheduled));
        await assignmentRepo.SetForProbeAsync(new ProbeId("probe-1"), ["plugin-http"]);

        var useCase = new TriggerProbeActionUseCase(probeRepo, pluginRepo, assignmentRepo, executionRepo, unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new TriggerProbeActionInput
        {
            ProbeId = "probe-1",
            PluginId = "plugin-http",
            TriggeredBy = "admin"
        }));
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
