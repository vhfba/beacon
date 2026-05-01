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
public class ProbeMutations
{
    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("recordProbeHeartbeat")]
    public async Task<ProbeHeartbeatResponse> RecordProbeHeartbeatAsync(
        ProbeHeartbeatInputType input,
        [Service] RecordProbeHeartbeatUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new ProbeHeartbeatResponse
            {
                Success = true,
                AutoRegistered = result.AutoRegistered,
                Probe = result.Probe.ToGraphQLType(),
                Runtime = result.Runtime.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new ProbeHeartbeatResponse
            {
                Success = false,
                AutoRegistered = false,
                Message = ex.Message
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("reportProbeMetrics")]
    public async Task<ReportProbeMetricsResponse> ReportProbeMetricsAsync(
        ReportProbeMetricsInputType input,
        [Service] ReportProbeMetricsUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new ReportProbeMetricsResponse
            {
                Success = true,
                ProbeId = result.ProbeId,
                AcceptedSamples = result.AcceptedSamples,
                ReceivedAtUtc = result.ReceivedAtUtc
            };
        }
        catch (DomainException ex)
        {
            return new ReportProbeMetricsResponse
            {
                Success = false,
                Message = ex.Message,
                ProbeId = input.ProbeId
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("updateProbeStatus")]
    public async Task<UpdateProbeStatusResponse> UpdateProbeStatusAsync(
        string probeId,
        string status,
        [Service] UpdateProbeStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var probe = await useCase.ExecuteAsync(probeId, status, cancellationToken);
            return new UpdateProbeStatusResponse
            {
                Success = true,
                Probe = probe.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new UpdateProbeStatusResponse
            {
                Success = false,
                Message = ex.Message,
                Probe = null
            };
        }
    }

    [Authorize(Policy = AuthorizationPolicies.ProbeOrAdmin)]
    [GraphQLName("updateProbeActionStatus")]
    public async Task<UpdateProbeActionStatusResponse> UpdateProbeActionStatusAsync(
        UpdateProbeActionStatusInputType input,
        [Service] UpdateProbeActionStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new UpdateProbeActionStatusResponse
            {
                Success = true,
                Execution = updated.ToGraphQLType()
            };
        }
        catch (DomainException ex)
        {
            return new UpdateProbeActionStatusResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
