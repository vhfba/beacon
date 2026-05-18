namespace CentralServer.Tests.Unit.UseCases;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Tests.Support;

public class UpdateProbeTestConfigUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithScheduledPluginId_CreatesConfig()
    {
        var probes = new InMemoryProbeRepository();
        var testTypes = new InMemoryTestTypeRepository();
        var plugins = new InMemoryPluginRepository();
        var configs = new InMemoryProbeTestConfigurationRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var probe = new Probe(new ProbeId("probe-a"), "Probe A", "Building A", "10.0.0.1");
        await probes.RegisterAsync(probe);
        await plugins.CreateAsync(new Plugin("WIFI", "Wi-Fi", "1.0.0", "checksum", "Wireless checks"));

        var useCase = new UpdateProbeTestConfigUseCase(probes, testTypes, plugins, configs, unitOfWork);

        var result = await useCase.ExecuteAsync(new()
        {
            ProbeId = "probe-a",
            TestType = "WIFI",
            IntervalSeconds = 30,
            Enabled = true
        });

        Assert.Equal("WIFI", result.TestType);
        Assert.True(result.Enabled);
        Assert.NotNull(await testTypes.GetByNameAsync("WIFI"));
    }

    [Fact]
    public async Task ExecuteAsync_WithActionPluginId_Throws()
    {
        var probes = new InMemoryProbeRepository();
        var testTypes = new InMemoryTestTypeRepository();
        var plugins = new InMemoryPluginRepository();
        var configs = new InMemoryProbeTestConfigurationRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var probe = new Probe(new ProbeId("probe-a"), "Probe A", "Building A", "10.0.0.1");
        await probes.RegisterAsync(probe);
        await plugins.CreateAsync(new Plugin("WIFI_SCAN_ACTION", "Scan", "1.0.0", "checksum", executionMode: PluginExecutionMode.Action));

        var useCase = new UpdateProbeTestConfigUseCase(probes, testTypes, plugins, configs, unitOfWork);

        var ex = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new()
        {
            ProbeId = "probe-a",
            TestType = "WIFI_SCAN_ACTION",
            IntervalSeconds = 30,
            Enabled = true
        }));

        Assert.Contains("does not support scheduled execution", ex.Message, StringComparison.Ordinal);
    }
}
