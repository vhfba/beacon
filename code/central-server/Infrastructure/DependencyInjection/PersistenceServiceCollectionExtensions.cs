namespace CentralServer.Infrastructure.DependencyInjection;

using CentralServer.Application.Abstractions;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence;
using CentralServer.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"]?.Trim().ToLowerInvariant();
        services.AddDbContext<CentralServerDbContext>(options =>
        {
            if (databaseProvider == "inmemory" || databaseProvider == "h2")
            {
                var databaseName = configuration["Database:InMemoryName"] ?? "beacon_central_dev";
                options.UseInMemoryDatabase(databaseName);
                return;
            }

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IProbeRepository, ProbeRepositoryAdapter>();
        services.AddScoped<ITestTypeRepository, TestTypeRepositoryAdapter>();
        services.AddScoped<IProbeTestConfigurationRepository, ProbeTestConfigurationRepositoryAdapter>();
        services.AddScoped<IPluginRepository, PluginRepositoryAdapter>();
        services.AddScoped<IProbePluginAssignmentRepository, ProbePluginAssignmentRepositoryAdapter>();
        services.AddScoped<IProbeActionExecutionRepository, ProbeActionExecutionRepositoryAdapter>();
        services.AddScoped<IUnitOfWork, CentralServerUnitOfWork>();

        return services;
    }
}
