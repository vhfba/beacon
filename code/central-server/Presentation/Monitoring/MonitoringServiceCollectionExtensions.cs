namespace CentralServer.Presentation.DependencyInjection;

using CentralServer.Application.Abstractions;
using CentralServer.Infrastructure.Monitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class MonitoringServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonitoringOptions>(
            configuration.GetSection(MonitoringOptions.SectionName));
        services.AddHttpClient<IGrafanaDashboardClient, GrafanaDashboardSyncService>();
        return services;
    }
}
