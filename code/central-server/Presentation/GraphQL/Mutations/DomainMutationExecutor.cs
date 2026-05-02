namespace CentralServer.Presentation.GraphQL;

using CentralServer.Domain.Models;

internal static class DomainMutationExecutor
{
    public static async Task<TResponse> ExecuteAsync<TResult, TResponse>(
        Func<Task<TResult>> execute,
        Func<TResult, TResponse> success,
        Func<string, TResponse> failure)
    {
        try
        {
            var result = await execute();
            return success(result);
        }
        catch (DomainException ex)
        {
            return failure(ex.Message);
        }
    }

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
