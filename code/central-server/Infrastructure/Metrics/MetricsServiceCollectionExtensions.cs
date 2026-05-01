namespace CentralServer.Infrastructure.DependencyInjection;

using CentralServer.Application.Abstractions;
using CentralServer.Infrastructure.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddMetricsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MetricsStoreOptions>(
            configuration.GetSection(MetricsStoreOptions.SectionName));

        var metricsProvider = configuration[$"{MetricsStoreOptions.SectionName}:Provider"]?.Trim().ToLowerInvariant();
        if (metricsProvider == "inmemory")
        {
            services.AddSingleton<IProbeMetricsStore, InMemoryProbeMetricsStore>();
            return services;
        }

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MetricsStoreOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.Redis.ConnectionString);
        });
        services.AddSingleton<IProbeMetricsStore, RedisProbeMetricsStore>();

        return services;
    }
}
