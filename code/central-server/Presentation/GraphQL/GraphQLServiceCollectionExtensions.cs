namespace CentralServer.Presentation.DependencyInjection;

using CentralServer.Presentation.GraphQL;
using CentralServer.Presentation.GraphQL.ErrorHandling;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class GraphQLServiceCollectionExtensions
{
    public static IRequestExecutorBuilder AddPresentationGraphQL(this IServiceCollection services)
    {
        return services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddErrorFilter<GraphQLErrorFilter>()
            .AddTypeExtension<ProbeQueries>()
            .AddTypeExtension<PluginQueries>()
            .AddTypeExtension<FleetQueries>()
            .AddTypeExtension<ProbeMutations>()
            .AddTypeExtension<PluginMutations>()
            .AddTypeExtension<ProbeAdministrationMutations>()
            .AddType<ProbeType>()
            .AddType<ProbeCoverageSummaryType>()
            .AddType<ProbeRuntimeType>()
            .AddType<ProbeStatusType>()
            .AddType<ProbeTestConfigType>()
            .AddType<ProbeConfigType>()
            .AddType<PluginType>()
            .AddType<PluginExecutionModeType>()
            .AddType<ProbePluginAssignmentType>()
            .AddType<ProbeActionExecutionType>()
            .AddType<ProbeActionExecutionStatusType>()
            .AddType<ProbeControlCommandType>()
            .AddType<ProbeControlCommandStatusType>()
            .AddType<ProbeControlCommandTypeType>()
            .AddType<FleetStatusResponse>()
            .AddType<ProbeHeartbeatResponse>()
            .AddType<RegisterPluginResponse>()
            .AddType<ReportProbeMetricsResponse>()
            .AddType<UpdateProbeTestConfigResponse>()
            .AddType<UpdateProbeActionStatusResponse>()
            .AddType<UpdateProbeStatusResponse>()
            .AddType<SetProbeTestEnabledResponse>()
            .AddType<SetPluginAvailabilityResponse>()
            .AddType<SetProbePluginsResponse>()
            .AddType<DeleteProbeResponse>()
            .AddType<DeletePluginResponse>()
            .AddType<TriggerProbeActionResponse>()
            .AddType<ProbeControlCommandResponse>()
            .AddType<UpdateProbeProfileResponse>();
    }
}
