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

    public string AdminApiKey => "test-admin-key";
    public string ProbeApiKey => "test-probe-key";
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
                ["GraphQL:EnableIntrospection"] = "false",
                ["GraphQL:MaxQueryDepth"] = "8",
                ["GraphQL:MaxQueryComplexity"] = "50"
            });
        });
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
