namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
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
        try
        {
            var config = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new UpdateProbeTestConfigResponse
            {
                Success = true,
                Config = config.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new UpdateProbeTestConfigResponse
            {
                Success = false,
                Message = ex.Message,
                Config = null
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("setProbeTestEnabled")]
    public async Task<SetProbeTestEnabledResponse> SetProbeTestEnabledAsync(
        SetProbeTestEnabledInputType input,
        [Service] SetProbeTestEnabledUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new SetProbeTestEnabledResponse
            {
                Success = true,
                Config = config.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new SetProbeTestEnabledResponse
            {
                Success = false,
                Message = ex.Message,
                Config = null
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("setProbePlugins")]
    public async Task<SetProbePluginsResponse> SetProbePluginsAsync(
        SetProbePluginsInputType input,
        [Service] SetProbePluginsUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignments = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new SetProbePluginsResponse
            {
                Success = true,
                Assignments = assignments.Select(a => a.ToGraphQLType()).ToList()
            };
        }
        catch (DomainException ex)
        {
            return new SetProbePluginsResponse
            {
                Success = false,
                Message = ex.Message,
                Assignments = []
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("deleteProbe")]
    public async Task<DeleteProbeResponse> DeleteProbeAsync(
        string probeId,
        [Service] DeleteProbeUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            await useCase.ExecuteAsync(probeId, cancellationToken);
            return new DeleteProbeResponse
            {
                Success = true,
                ProbeId = probeId
            };
        }
        catch (DomainException ex)
        {
            return new DeleteProbeResponse
            {
                Success = false,
                Message = ex.Message,
                ProbeId = probeId
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("triggerProbeAction")]
    public async Task<TriggerProbeActionResponse> TriggerProbeActionAsync(
        TriggerProbeActionInputType input,
        [Service] TriggerProbeActionUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var execution = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new TriggerProbeActionResponse
            {
                Success = true,
                Execution = execution.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new TriggerProbeActionResponse
            {
                Success = false,
                Message = ex.Message,
                Execution = null
            };
        }
    }
}
