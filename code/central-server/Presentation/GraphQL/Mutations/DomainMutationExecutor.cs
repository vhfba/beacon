namespace CentralServer.Presentation.GraphQL;

using CentralServer.Domain.Models;

internal static class DomainMutationExecutor
{
    public static async Task<TResponse> ExecuteAsync<TResponse>(
        Func<Task<TResponse>> execute,
        Func<string, TResponse> failure)
    {
        try
        {
            return await execute();
        }
        catch (DomainException ex)
        {
            return failure(ex.Message);
        }
    }
}
