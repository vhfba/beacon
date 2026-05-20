namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.DTOs;
using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class ProbeControlCommandUseCaseTests
{
    [Fact]
    public async Task RequestWifiConnect_RedactsPasswordInReturnedPayload()
    {
        var probes = new InMemoryProbeRepository();
        var commands = new InMemoryProbeControlCommandRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probes.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe", "Lab", "10.0.0.1"));

        var useCase = new RequestWifiConnectUseCase(probes, commands, unitOfWork);
        var result = await useCase.ExecuteAsync(new RequestWifiConnectInput
        {
            ProbeId = "probe-1",
            Ssid = "eduroam",
            Password = "secret-password",
            RequestedBy = "admin-ui"
        });

        Assert.Contains("\"password\":\"***\"", result.PayloadJson);
        Assert.DoesNotContain("secret-password", result.PayloadJson);
    }

    [Fact]
    public async Task PendingCommands_ReturnsUnredactedPayloadForProbe()
    {
        var probes = new InMemoryProbeRepository();
        var commands = new InMemoryProbeControlCommandRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probes.RegisterAsync(new Probe(new ProbeId("probe-1"), "Probe", "Lab", "10.0.0.1"));
        await new RequestWifiConnectUseCase(probes, commands, unitOfWork).ExecuteAsync(new RequestWifiConnectInput
        {
            ProbeId = "probe-1",
            Ssid = "eduroam",
            Password = "secret-password",
            RequestedBy = "admin-ui"
        });

        var result = await new GetPendingProbeControlCommandsUseCase(probes, commands, unitOfWork)
            .ExecuteAsync("probe-1", 10);

        var command = Assert.Single(result);
        Assert.Equal(ProbeControlCommandStatus.Delivered, command.Status);
        Assert.Contains("secret-password", command.PayloadJson);
    }

    [Fact]
    public async Task UpdateProbeProfile_DoesNotChangeTechnicalId()
    {
        var probes = new InMemoryProbeRepository();
        var commands = new InMemoryProbeControlCommandRepository();
        var unitOfWork = new NoOpUnitOfWork();
        await probes.RegisterAsync(new Probe(new ProbeId("probe-1"), "Old", "Old Lab", "10.0.0.1"));

        var result = await new UpdateProbeProfileUseCase(probes, commands, unitOfWork)
            .ExecuteAsync(new UpdateProbeProfileInput
            {
                ProbeId = "probe-1",
                Name = "New Name",
                Location = "New Lab",
                RequestedBy = "admin-ui"
            });

        Assert.Equal("probe-1", result.Probe.Id);
        Assert.Equal("New Name", result.Probe.Name);
        Assert.Equal("New Lab", result.Probe.Location);
        Assert.Equal(ProbeControlCommandType.UpdateProfile, result.Command.Type);
    }

    [Fact]
    public async Task Heartbeat_UpdatesObservedFieldsButKeepsAdminProfile()
    {
        var probes = new InMemoryProbeRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var probe = new Probe(new ProbeId("probe-1"), "Admin Name", "Admin Lab", "10.0.0.1");
        await probes.RegisterAsync(probe);

        var coordinator = new CentralServer.Application.Services.ProbeRuntimeCoordinator(probes, unitOfWork);
        await coordinator.RecordHeartbeatAsync(new ProbeHeartbeatInput
        {
            ProbeId = "probe-1",
            Name = "Reported Name",
            Location = "Reported Lab",
            IpAddress = "10.0.0.2",
            Ssid = "BEACON",
            AgentVersion = "pi-agent"
        });

        var updated = await probes.GetByIdAsync(new ProbeId("probe-1"));
        Assert.Equal("Admin Name", updated!.Name);
        Assert.Equal("Admin Lab", updated.Location);
        Assert.Equal("10.0.0.2", updated.IpAddress);
        Assert.Equal("BEACON", updated.Ssid);
    }
}
