namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Application.Services;
using CentralServer.Presentation.GraphQL.Mappings;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Security;
using HotChocolate;
using HotChocolate.Authorization;

[ExtendObjectType(typeof(Mutation))]
public class PluginMutations
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("registerPlugin")]
    public async Task<RegisterPluginResponse> RegisterPluginAsync(
        RegisterPluginInputType input,
        [Service] RegisterPluginUseCase useCase,
        [Service] PluginDashboardAutomationService dashboardAutomationService,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            async () =>
            {
                if (!string.IsNullOrWhiteSpace(input.DashboardJson))
                {
                    dashboardAutomationService.ValidateDashboardJson(input.DashboardJson);
                }

                var plugin = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
                string? message = null;

                if (!string.IsNullOrWhiteSpace(input.DashboardJson))
                {
                    var summary = await dashboardAutomationService.ApplyDashboardJsonAsync(
                        plugin.Id,
                        input.DashboardJson,
                        cancellationToken);

                    message = $"Plugin registered. Grafana dashboard sync {(summary.GrafanaApplied > 0 ? "applied" : "failed/skipped")} for UID '{summary.DashboardUid}'. {summary.Message}";
                }

                return new RegisterPluginResponse
                {
                    Success = true,
                    Message = message,
                    Plugin = plugin.ToGraphQLType()
                };
            },
            message => new RegisterPluginResponse
            {
                Success = false,
                Message = message,
                Plugin = null
            });
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("setPluginAvailability")]
    public async Task<SetPluginAvailabilityResponse> SetPluginAvailabilityAsync(
        SetPluginAvailabilityInputType input,
        [Service] SetPluginAvailabilityUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            async () =>
            {
                var plugin = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
                return new SetPluginAvailabilityResponse
                {
                    Success = true,
                    Plugin = plugin.ToGraphQLType()
                };
            },
            message => new SetPluginAvailabilityResponse
            {
                Success = false,
                Message = message,
                Plugin = null
            });
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("deletePlugin")]
    public async Task<DeletePluginResponse> DeletePluginAsync(
        string pluginId,
        [Service] DeletePluginUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            async () =>
            {
                await useCase.ExecuteAsync(pluginId, cancellationToken);
                return new DeletePluginResponse
                {
                    Success = true,
                    PluginId = pluginId
                };
            },
            message => new DeletePluginResponse
            {
                Success = false,
                Message = message,
                PluginId = pluginId
            });
    }
}
