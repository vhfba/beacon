namespace CentralServer.Tests.Integration;

using System.Net.Http.Headers;
using CentralServer.Application.Abstractions;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class CentralServerWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"beacon_test_{Guid.NewGuid():N}";
    private readonly string _bundleDirectory = Path.Combine(Path.GetTempPath(), $"beacon_test_bundles_{Guid.NewGuid():N}");

    public string AdminApiKey => "test-admin-key";
    public string ProbeApiKey => "test-probe-key";
    public string BundleDirectory => _bundleDirectory;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Database:InMemoryName"] = _databaseName,
                ["Metrics:Provider"] = "InMemory",
                ["Auth:AdminApiKey"] = AdminApiKey,
                ["Auth:ProbeApiKey"] = ProbeApiKey,
                ["Plugins:BundleDirectory"] = _bundleDirectory,
                ["GraphQL:EnableIntrospection"] = "false",
                ["GraphQL:MaxQueryDepth"] = "8",
                ["GraphQL:MaxQueryComplexity"] = "50"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_bundleDirectory))
        {
            Directory.Delete(_bundleDirectory, recursive: true);
        }
    }

    public async Task SeedProbeAsync(string id, string ipAddress, ProbeStatus status)
    {
        using var scope = Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProbeRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var probe = new Probe(new ProbeId(id), $"Name-{id}", "Test-Site", ipAddress);
        if (status != ProbeStatus.Registered)
        {
            probe.UpdateStatus(status);
        }

        await repository.RegisterAsync(probe);
        await unitOfWork.SaveChangesAsync();
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", AdminApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public HttpClient CreateProbeClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ProbeApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
