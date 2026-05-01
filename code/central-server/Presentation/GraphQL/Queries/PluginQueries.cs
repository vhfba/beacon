namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Presentation.GraphQL.Mappings;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Security;
using HotChocolate;
using HotChocolate.Authorization;

[ExtendObjectType(typeof(Query))]
public class PluginQueries
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("plugins")]
    public async Task<List<PluginType>> GetPluginsAsync(
        [Service] ListPluginsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var plugins = await useCase.ExecuteAsync(cancellationToken);
        return plugins.Select(p => p.ToGraphQLType()).ToList();
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [GraphQLName("plugin")]
    public async Task<PluginType?> GetPluginByIdAsync(
        string id,
        [Service] GetPluginByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var plugin = await useCase.ExecuteAsync(id, cancellationToken);
        return plugin?.ToGraphQLType();
    }
}
