namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;
using HotChocolate.Execution;
[GraphQLName("Query")]
public class Query
{
    [GraphQLName("fleetStatus")]
    public async Task<FleetStatusResponse> GetFleetStatusAsync(
        [Service] GetFleetStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var probes = await useCase.ExecuteAsync(cancellationToken);
            return new FleetStatusResponse
            {
                Probes = probes.Select(ProbeType.FromDTO).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to get fleet status: {ex.Message}");
        }
    }

    [GraphQLName("probeConfig")]
    public async Task<ProbeConfigType> GetProbeConfigAsync(
        string probeId,
        [Service] GetProbeConfigUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = await useCase.ExecuteAsync(probeId, cancellationToken);
            return ProbeConfigType.FromDTO(config);
        }
        catch (DomainException ex)
        {
            throw new GraphQLException($"Domain error: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to get probe config: {ex.Message}");
        }
    }

    [GraphQLName("plugins")]
    public async Task<List<PluginType>> GetPluginsAsync(
        [Service] ListPluginsUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var plugins = await useCase.ExecuteAsync(cancellationToken);
            return plugins.Select(PluginType.FromDTO).ToList();
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to list plugins: {ex.Message}");
        }
    }

    [GraphQLName("plugin")]
    public async Task<PluginType?> GetPluginByIdAsync(
        string id,
        [Service] GetPluginByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await useCase.ExecuteAsync(id, cancellationToken);
            return plugin == null ? null : PluginType.FromDTO(plugin);
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to get plugin: {ex.Message}");
        }
    }
}
