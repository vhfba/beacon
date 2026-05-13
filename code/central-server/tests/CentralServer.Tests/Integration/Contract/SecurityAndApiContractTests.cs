namespace CentralServer.Tests.Integration.Contract;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CentralServer.Tests.Integration;

public class SecurityAndApiContractTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public SecurityAndApiContractTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectGraphQLAdminAndPluginBundleEndpoints()
    {
        var anonymous = _factory.CreateClient();
        var probe = _factory.CreateProbeClient();
        var admin = _factory.CreateAdminClient();

        var noKeyGraphQl = await anonymous.PostAsJsonAsync("/graphql", new { query = "query { fleetStatus { probes { id } } }" });
        Assert.Equal(HttpStatusCode.Unauthorized, noKeyGraphQl.StatusCode);

        var noKeyBundle = await anonymous.GetAsync("/plugins/PING/1.0.0/bundle");
        Assert.Equal(HttpStatusCode.Unauthorized, noKeyBundle.StatusCode);

        var probeAdminMutation = await probe.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($input: RegisterPluginInputTypeInput!) {
                  registerPlugin(input: $input) { success }
                }
                """,
            variables = new
            {
                input = new
                {
                    id = "PROBE_FORBIDDEN",
                    name = "Forbidden",
                    version = "1.0.0",
                    checksum = "checksum",
                    executionMode = "SCHEDULED"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, probeAdminMutation.StatusCode);
        var forbiddenPayload = await probeAdminMutation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(forbiddenPayload.TryGetProperty("errors", out _));

        var adminMutation = await admin.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation($input: RegisterPluginInputTypeInput!) {
                  registerPlugin(input: $input) { success plugin { id } }
                }
                """,
            variables = new
            {
                input = new
                {
                    id = "ADMIN_ALLOWED",
                    name = "Admin Allowed",
                    version = "1.0.0",
                    checksum = "checksum",
                    executionMode = "SCHEDULED"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, adminMutation.StatusCode);
        var adminPayload = await adminMutation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(adminPayload.GetProperty("data").GetProperty("registerPlugin").GetProperty("success").GetBoolean());
    }
}
