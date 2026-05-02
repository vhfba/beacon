namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Presentation.GraphQL.Mappings;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Security;
using HotChocolate;
using HotChocolate.Authorization;

[ExtendObjectType(typeof(Mutation))]
public class ProbeAdministrationMutations
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("updateProbeTestConfig")]
    public async Task<UpdateProbeTestConfigResponse> UpdateProbeTestConfigAsync(
        UpdateProbeTestConfigInputType input,
        [Service] UpdateProbeTestConfigUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            config => new UpdateProbeTestConfigResponse
            {
                Success = true,
                Config = config.ToGraphQLType()
            },
            message => new UpdateProbeTestConfigResponse
            {
                Success = false,
                Message = message,
                Config = null
            });
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("setProbeTestEnabled")]
    public async Task<SetProbeTestEnabledResponse> SetProbeTestEnabledAsync(
        SetProbeTestEnabledInputType input,
        [Service] SetProbeTestEnabledUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            config => new SetProbeTestEnabledResponse
            {
                Success = true,
                Config = config.ToGraphQLType()
            },
            message => new SetProbeTestEnabledResponse
            {
                Success = false,
                Message = message,
                Config = null
            });
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("setProbePlugins")]
    public async Task<SetProbePluginsResponse> SetProbePluginsAsync(
        SetProbePluginsInputType input,
        [Service] SetProbePluginsUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            assignments => new SetProbePluginsResponse
            {
                Success = true,
                Assignments = assignments.Select(a => a.ToGraphQLType()).ToList()
            },
            message => new SetProbePluginsResponse
            {
                Success = false,
                Message = message,
                Assignments = []
            });
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("deleteProbe")]
    public async Task<DeleteProbeResponse> DeleteProbeAsync(
        string probeId,
        [Service] DeleteProbeUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            async () =>
            {
                await useCase.ExecuteAsync(probeId, cancellationToken);
                return new DeleteProbeResponse
                {
                    Success = true,
                    ProbeId = probeId
                };
            },
            message => new DeleteProbeResponse
            {
                Success = false,
                Message = message,
                ProbeId = probeId
            });
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("triggerProbeAction")]
    public async Task<TriggerProbeActionResponse> TriggerProbeActionAsync(
        TriggerProbeActionInputType input,
        [Service] TriggerProbeActionUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            execution => new TriggerProbeActionResponse
            {
                Success = true,
                Execution = execution.ToGraphQLType()
            },
            message => new TriggerProbeActionResponse
            {
                Success = false,
                Message = message,
                Execution = null
            });
    }
}
