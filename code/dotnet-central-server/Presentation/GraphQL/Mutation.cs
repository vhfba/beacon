namespace CentralServer.Presentation.GraphQL;

using CentralServer.Application.UseCases;
using CentralServer.Domain.Models;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using HotChocolate;
using HotChocolate.Execution;
[GraphQLName("Mutation")]
public class Mutation
{
    [GraphQLName("registerProbe")]
    public async Task<RegisterProbeResponse> RegisterProbeAsync(
        RegisterProbeInputType input,
        [Service] RegisterProbeUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var probe = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new RegisterProbeResponse
            {
                Success = true,
                Probe = ProbeType.FromDTO(probe)
            };
        }
        catch (ProbeRegistrationException ex)
        {
            return new RegisterProbeResponse
            {
                Success = false,
                Message = ex.Message,
                Probe = null
            };
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to register probe: {ex.Message}");
        }
    }

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
                Config = ProbeTestConfigType.FromDTO(config)
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
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to update probe test config: {ex.Message}");
        }
    }

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
                Probe = ProbeType.FromDTO(probe)
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
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to update probe status: {ex.Message}");
        }
    }

    [GraphQLName("registerPlugin")]
    public async Task<RegisterPluginResponse> RegisterPluginAsync(
        RegisterPluginInputType input,
        [Service] RegisterPluginUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await useCase.ExecuteAsync(input.ToDTO(), cancellationToken);
            return new RegisterPluginResponse
            {
                Success = true,
                Plugin = PluginType.FromDTO(plugin)
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
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to register plugin: {ex.Message}");
        }
    }

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
                Config = ProbeTestConfigType.FromDTO(config)
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
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to set probe test enabled state: {ex.Message}");
        }
    }

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
                Plugin = PluginType.FromDTO(plugin)
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
        catch (Exception ex)
        {
            throw new GraphQLException($"Failed to set plugin availability: {ex.Message}");
        }
    }
}
