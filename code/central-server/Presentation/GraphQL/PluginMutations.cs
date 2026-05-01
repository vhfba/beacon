namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Application.Services;
using CentralServer.Domain.Models;
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
        try
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
        }
        catch (DomainException ex)
        {
            return new RegisterPluginResponse
            {
                Success = false,
                Message = ex.Message,
                Plugin = null
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("setPluginAvailability")]
    public async Task<SetPluginAvailabilityResponse> SetPluginAvailabilityAsync(
        SetPluginAvailabilityInputType input,
        [Service] SetPluginAvailabilityUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new SetPluginAvailabilityResponse
            {
                Success = true,
                Plugin = plugin.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new SetPluginAvailabilityResponse
            {
                Success = false,
                Message = ex.Message,
                Plugin = null
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("deletePlugin")]
    public async Task<DeletePluginResponse> DeletePluginAsync(
        string pluginId,
        [Service] DeletePluginUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            await useCase.ExecuteAsync(pluginId, cancellationToken);
            return new DeletePluginResponse
            {
                Success = true,
                PluginId = pluginId
            };
        }
        catch (DomainException ex)
        {
            return new DeletePluginResponse
            {
                Success = false,
                Message = ex.Message,
                PluginId = pluginId
            };
        }
    }
}
