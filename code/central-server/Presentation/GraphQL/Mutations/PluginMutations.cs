namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.DTOs;
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
            () => RegisterPluginWithDashboardAsync(input, useCase, dashboardAutomationService, cancellationToken),
            result => new RegisterPluginResponse
            {
                Success = true,
                Message = result.Message,
                Plugin = result.Plugin.ToGraphQLType()
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
        [Service] PluginDashboardAutomationService dashboardAutomationService,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => SetPluginAvailabilityWithDashboardAsync(input, useCase, dashboardAutomationService, cancellationToken),
            result => new SetPluginAvailabilityResponse
            {
                Success = true,
                Message = result.Message,
                Plugin = result.Plugin.ToGraphQLType()
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
        [Service] PluginDashboardAutomationService dashboardAutomationService,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            async () =>
            {
                var plugin = await useCase.ExecuteAsync(pluginId, cancellationToken);
                string? message = null;
                if (!string.IsNullOrWhiteSpace(plugin.DashboardJson))
                {
                    var summary = await dashboardAutomationService.RemoveDashboardAsync(plugin.Id, cancellationToken);
                    message = $"Plugin deleted. Grafana dashboard removal {(summary.GrafanaApplied > 0 ? "applied" : "failed/skipped")} for UID '{summary.DashboardUid}'. {summary.Message}";
                }

                return new DeletePluginResponse
                {
                    Success = true,
                    Message = message,
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

    private static async Task<SetPluginAvailabilityResult> SetPluginAvailabilityWithDashboardAsync(
        SetPluginAvailabilityInputType input,
        SetPluginAvailabilityUseCase useCase,
        PluginDashboardAutomationService dashboardAutomationService,
        CancellationToken cancellationToken)
    {
        var plugin = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
        string? message = null;

        if (!string.IsNullOrWhiteSpace(plugin.DashboardJson))
        {
            if (plugin.Available)
            {
                var summary = await dashboardAutomationService.ApplyDashboardJsonAsync(
                    plugin.Id,
                    plugin.DashboardJson,
                    cancellationToken);
                message = $"Plugin enabled. Grafana dashboard sync {(summary.GrafanaApplied > 0 ? "applied" : "failed/skipped")} for UID '{summary.DashboardUid}'. {summary.Message}";
            }
            else
            {
                var summary = await dashboardAutomationService.RemoveDashboardAsync(plugin.Id, cancellationToken);
                message = $"Plugin disabled. Grafana dashboard removal {(summary.GrafanaApplied > 0 ? "applied" : "failed/skipped")} for UID '{summary.DashboardUid}'. {summary.Message}";
            }
        }

        return new SetPluginAvailabilityResult(plugin, message);
    }

    private static async Task<RegisterPluginResult> RegisterPluginWithDashboardAsync(
        RegisterPluginInputType input,
        RegisterPluginUseCase useCase,
        PluginDashboardAutomationService dashboardAutomationService,
        CancellationToken cancellationToken)
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

        return new RegisterPluginResult(plugin, message);
    }

    private sealed record SetPluginAvailabilityResult(PluginDTO Plugin, string? Message);

    private sealed record RegisterPluginResult(PluginDTO Plugin, string? Message);
}
