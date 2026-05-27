namespace CentralServer.Tests.Integration.Contract;

using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CentralServer.Tests.Integration;

public class PluginBundleContractTests : IClassFixture<CentralServerWebAppFactory>
{
    private readonly CentralServerWebAppFactory _factory;

    public PluginBundleContractTests(CentralServerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BundleUpload_WithProbeKey_IsForbidden()
    {
        var probe = _factory.CreateProbeClient();
        using var form = BundleForm("payload");

        var response = await probe.PostAsync("/plugins/PING/1.0.0/bundle", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BundleUpload_WithAdminKey_StoresZipAndReturnsChecksum()
    {
        var admin = _factory.CreateAdminClient();
        var payload = Encoding.UTF8.GetBytes("fake zip payload");
        using var form = BundleForm(payload);

        var response = await admin.PostAsync("/plugins/PING/1.0.0/bundle", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var expectedChecksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        Assert.Equal("PING-1.0.0.zip", body.GetProperty("fileName").GetString());
        Assert.Equal(expectedChecksum, body.GetProperty("checksum").GetString());
        Assert.True(File.Exists(Path.Combine(_factory.BundleDirectory, "PING-1.0.0.zip")));
    }

    [Fact]
    public async Task BundleDownload_WhenPluginAvailableAndBundleExists_ReturnsZip()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        await RegisterPluginAsync(admin, "BUNDLE_OK", available: true);
        await File.WriteAllTextAsync(Path.Combine(_factory.BundleDirectory, "BUNDLE_OK-1.0.0.zip"), "bundle-content");

        var response = await probe.GetAsync("/plugins/BUNDLE_OK/1.0.0/bundle");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("BUNDLE_OK-1.0.0.zip", response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Equal("bundle-content", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BundleDownload_WhenPluginUnavailableOrVersionDiffers_ReturnsNotFound()
    {
        var admin = _factory.CreateAdminClient();
        var probe = _factory.CreateProbeClient();
        await RegisterPluginAsync(admin, "BUNDLE_UNAVAILABLE", available: false);
        await File.WriteAllTextAsync(Path.Combine(_factory.BundleDirectory, "BUNDLE_UNAVAILABLE-1.0.0.zip"), "bundle-content");

        var unavailable = await probe.GetAsync("/plugins/BUNDLE_UNAVAILABLE/1.0.0/bundle");
        var wrongVersion = await probe.GetAsync("/plugins/BUNDLE_UNAVAILABLE/2.0.0/bundle");

        Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, wrongVersion.StatusCode);
    }

    [Fact]
    public async Task BundleDownload_WithUnsafeSegment_ReturnsBadRequest()
    {
        var probe = _factory.CreateProbeClient();

        var response = await probe.GetAsync("/plugins/BAD!/1.0.0/bundle");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent BundleForm(string content)
    {
        return BundleForm(Encoding.UTF8.GetBytes(content));
    }

    private static MultipartFormDataContent BundleForm(byte[] content)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(file, "bundle", "plugin.zip");
        return form;
    }

    private static async Task RegisterPluginAsync(HttpClient admin, string pluginId, bool available)
    {
        await IntegrationTestClient.PostGraphQLAsync(admin, """
            mutation($input: RegisterPluginInputTypeInput!) {
              registerPlugin(input: $input) { success plugin { id } }
            }
            """, new
        {
            input = new
            {
                id = pluginId,
                name = pluginId,
                version = "1.0.0",
                checksum = $"checksum-{pluginId}",
                executionMode = "SCHEDULED"
            }
        });

        if (!available)
        {
            await IntegrationTestClient.PostGraphQLAsync(admin, """
                mutation($input: SetPluginAvailabilityInputTypeInput!) {
                  setPluginAvailability(input: $input) { success plugin { id available } }
                }
                """, new { input = new { pluginId, available = false } });
        }
    }
}
