namespace CentralServer.Tests.Integration.BlackBox;

using CentralServer.Domain.Repositories;
using CentralServer.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

public class ProbeRegistrationFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public ProbeRegistrationFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RepeatedHeartbeat_UpdatesExistingProbeWithoutDuplicating()
    {
        var client = _factory.CreateProbeClient();

        var first = await IntegrationTestClient.PostGraphQLAsync(client, """
            mutation($input: ProbeHeartbeatInputTypeInput!) {
              recordProbeHeartbeat(input: $input) {
                success
                autoRegistered
                probe { id ipAddress status }
              }
            }
            """, new
        {
            input = IntegrationTestClient.HeartbeatInput("probe-repeat", "10.10.0.1")
        });

        var second = await IntegrationTestClient.PostGraphQLAsync(client, """
            mutation($input: ProbeHeartbeatInputTypeInput!) {
              recordProbeHeartbeat(input: $input) {
                success
                autoRegistered
                probe { id ipAddress status }
              }
            }
            """, new
        {
            input = IntegrationTestClient.HeartbeatInput("probe-repeat", "10.10.0.2")
        });

        var firstHeartbeat = first.GetProperty("data").GetProperty("recordProbeHeartbeat");
        var secondHeartbeat = second.GetProperty("data").GetProperty("recordProbeHeartbeat");

        Assert.True(firstHeartbeat.GetProperty("autoRegistered").GetBoolean());
        Assert.False(secondHeartbeat.GetProperty("autoRegistered").GetBoolean());
        Assert.Equal("10.10.0.2", secondHeartbeat.GetProperty("probe").GetProperty("ipAddress").GetString());

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProbeRepository>();
        var probes = await repository.GetAllAsync();
        Assert.Single(probes, p => p.Id.Value == "probe-repeat");
    }
}
