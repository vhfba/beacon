namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Presentation.GraphQL.Mappings;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Security;
using HotChocolate;
using HotChocolate.Authorization;

[ExtendObjectType(typeof(Mutation))]
public class ProbeMutations
{
    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("recordProbeHeartbeat")]
    public async Task<ProbeHeartbeatResponse> RecordProbeHeartbeatAsync(
        ProbeHeartbeatInputType input,
        [Service] RecordProbeHeartbeatUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            result => new ProbeHeartbeatResponse
            {
                Success = true,
                AutoRegistered = result.AutoRegistered,
                Probe = result.Probe.ToGraphQLType(),
                Runtime = result.Runtime.ToGraphQLType()
            },
            message => new ProbeHeartbeatResponse
            {
                Success = false,
                AutoRegistered = false,
                Message = message
            });
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("reportProbeMetrics")]
    public async Task<ReportProbeMetricsResponse> ReportProbeMetricsAsync(
        ReportProbeMetricsInputType input,
        [Service] ReportProbeMetricsUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            result => new ReportProbeMetricsResponse
            {
                Success = true,
                ProbeId = result.ProbeId,
                AcceptedSamples = result.AcceptedSamples,
                ReceivedAtUtc = result.ReceivedAtUtc
            },
            message => new ReportProbeMetricsResponse
            {
                Success = false,
                Message = message,
                ProbeId = input.ProbeId
            });
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("updateProbeStatus")]
    public async Task<UpdateProbeStatusResponse> UpdateProbeStatusAsync(
        string probeId,
        string status,
        [Service] UpdateProbeStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(probeId, status, cancellationToken),
            probe => new UpdateProbeStatusResponse
            {
                Success = true,
                Probe = probe.ToGraphQLType()
            },
            message => new UpdateProbeStatusResponse
            {
                Success = false,
                Message = message,
                Probe = null
            });
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("updateProbeActionStatus")]
    public async Task<UpdateProbeActionStatusResponse> UpdateProbeActionStatusAsync(
        UpdateProbeActionStatusInputType input,
        [Service] UpdateProbeActionStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            updated => new UpdateProbeActionStatusResponse
            {
                Success = true,
                Execution = updated.ToGraphQLType()
            },
            message => new UpdateProbeActionStatusResponse
            {
                Success = false,
                Message = message
            });
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("updateProbeControlCommandStatus")]
    public async Task<ProbeControlCommandResponse> UpdateProbeControlCommandStatusAsync(
        UpdateProbeControlCommandStatusInputType input,
        [Service] UpdateProbeControlCommandStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await DomainMutationExecutor.ExecuteAsync(
            () => useCase.ExecuteAsync(input.ToDTO(), cancellationToken),
            command => new ProbeControlCommandResponse
            {
                Success = true,
                Command = command.ToGraphQLType()
            },
            message => new ProbeControlCommandResponse
            {
                Success = false,
                Message = message
            });
    }
}
