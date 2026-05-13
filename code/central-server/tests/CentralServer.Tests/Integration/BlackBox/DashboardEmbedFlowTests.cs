namespace CentralServer.Tests.Integration.BlackBox;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CentralServer.Tests.Integration;

public class DashboardEmbedFlowTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public DashboardEmbedFlowTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WithAdminKey_ReturnsExpectedShape()
    {
        var admin = _factory.CreateAdminClient();

        var response = await admin.PostAsJsonAsync("/monitoring/grafana/embed-session", new
        {
            site = "building-a",
            dashboardUid = "beacon-plugin-ping"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("building-a", payload.GetProperty("site").GetString());
        Assert.Equal("beacon-plugin-ping", payload.GetProperty("dashboardUid").GetString());
        Assert.Contains("beacon-plugin-ping", payload.GetProperty("embedUrl").GetString(), StringComparison.Ordinal);
        Assert.False(payload.GetProperty("grafanaSyncApplied").GetBoolean());
    }
}
