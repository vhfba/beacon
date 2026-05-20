namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Presentation.GraphQL.Mappings;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Security;
using HotChocolate;
using HotChocolate.Authorization;

[ExtendObjectType(typeof(Query))]
public class ProbeQueries
{
    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("probeConfig")]
    public async Task<ProbeConfigType> GetProbeConfigAsync(
        string probeId,
        [Service] GetProbeConfigUseCase useCase,
        [Service] RecordProbeConfigFetchUseCase recordProbeConfigFetchUseCase,
        CancellationToken cancellationToken)
    {
        var config = await useCase.ExecuteAsync(probeId, cancellationToken);
        await recordProbeConfigFetchUseCase.ExecuteAsync(probeId, cancellationToken);
        return config.ToGraphQLType();
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("probeRuntime")]
    public async Task<ProbeRuntimeType> GetProbeRuntimeAsync(
        string probeId,
        [Service] GetProbeRuntimeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var runtime = await useCase.ExecuteAsync(probeId, cancellationToken);
        return runtime.ToGraphQLType();
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("probePluginAssignments")]
    public async Task<List<ProbePluginAssignmentType>> GetProbePluginAssignmentsAsync(
        string probeId,
        [Service] GetProbePluginAssignmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var assignments = await useCase.ExecuteAsync(probeId, cancellationToken);
        return assignments.Select(a => a.ToGraphQLType()).ToList();
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("probeActionExecutions")]
    public async Task<List<ProbeActionExecutionType>> GetProbeActionExecutionsAsync(
        string probeId,
        int? limit,
        [Service] ListProbeActionExecutionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = limit.GetValueOrDefault(50);
        var executions = await useCase.ExecuteAsync(probeId, effectiveLimit <= 0 ? 50 : effectiveLimit, cancellationToken);
        return executions.Select(e => e.ToGraphQLType()).ToList();
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("pendingProbeActions")]
    public async Task<List<ProbeActionExecutionType>> GetPendingProbeActionsAsync(
        string probeId,
        int? limit,
        [Service] GetPendingProbeActionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var actions = await useCase.ExecuteAsync(probeId, limit ?? 10, cancellationToken);
        return actions.Select(a => a.ToGraphQLType()).ToList();
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("probeControlCommands")]
    public async Task<List<ProbeControlCommandType>> GetProbeControlCommandsAsync(
        string probeId,
        int? limit,
        [Service] ListProbeControlCommandsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var commands = await useCase.ExecuteAsync(probeId, limit.GetValueOrDefault(50), cancellationToken);
        return commands.Select(c => c.ToGraphQLType()).ToList();
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("pendingProbeControlCommands")]
    public async Task<List<ProbeControlCommandType>> GetPendingProbeControlCommandsAsync(
        string probeId,
        int? limit,
        [Service] GetPendingProbeControlCommandsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var commands = await useCase.ExecuteAsync(probeId, limit.GetValueOrDefault(10), cancellationToken);
        return commands.Select(c => c.ToGraphQLType()).ToList();
    }
}
