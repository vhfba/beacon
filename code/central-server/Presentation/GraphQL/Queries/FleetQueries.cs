namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Presentation.GraphQL.Mappings;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Security;
using HotChocolate;
using HotChocolate.Authorization;

[ExtendObjectType(typeof(Query))]
public class FleetQueries
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("fleetStatus")]
    public async Task<FleetStatusResponse> GetFleetStatusAsync(
        [Service] GetFleetStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        var probes = await useCase.ExecuteAsync(cancellationToken);
        return new FleetStatusResponse
        {
            Probes = probes.Select(p => p.ToGraphQLType()).ToList()
        };
    }
}
