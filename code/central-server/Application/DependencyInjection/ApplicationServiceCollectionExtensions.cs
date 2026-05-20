namespace CentralServer.Application.DependencyInjection;

using CentralServer.Application.Services;
using CentralServer.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ProbeRuntimeCoordinator>();
        services.AddScoped<PluginDashboardAutomationService>();

        services.AddScoped<GetFleetStatusUseCase>();
        services.AddScoped<GetProbeConfigUseCase>();
        services.AddScoped<RecordProbeConfigFetchUseCase>();
        services.AddScoped<GetProbeRuntimeUseCase>();
        services.AddScoped<RecordProbeHeartbeatUseCase>();
        services.AddScoped<ReportProbeMetricsUseCase>();
        services.AddScoped<ExportPrometheusMetricsUseCase>();
        services.AddScoped<GetFleetCoverageUseCase>();
        services.AddScoped<GetProbePluginAssignmentsUseCase>();
        services.AddScoped<UpdateProbeTestConfigUseCase>();
        services.AddScoped<UpdateProbeStatusUseCase>();
        services.AddScoped<ListPluginsUseCase>();
        services.AddScoped<RegisterPluginUseCase>();
        services.AddScoped<GetPluginByIdUseCase>();
        services.AddScoped<SetProbeTestEnabledUseCase>();
        services.AddScoped<SetPluginAvailabilityUseCase>();
        services.AddScoped<SetProbePluginsUseCase>();
        services.AddScoped<DeleteProbeUseCase>();
        services.AddScoped<DeletePluginUseCase>();
        services.AddScoped<TriggerProbeActionUseCase>();
        services.AddScoped<ListProbeActionExecutionsUseCase>();
        services.AddScoped<GetPendingProbeActionsUseCase>();
        services.AddScoped<UpdateProbeActionStatusUseCase>();
        services.AddScoped<ListProbeControlCommandsUseCase>();
        services.AddScoped<GetPendingProbeControlCommandsUseCase>();
        services.AddScoped<RequestWifiScanUseCase>();
        services.AddScoped<RequestWifiConnectUseCase>();
        services.AddScoped<UpdateProbeProfileUseCase>();
        services.AddScoped<UpdateProbeControlCommandStatusUseCase>();

        return services;
    }
}
