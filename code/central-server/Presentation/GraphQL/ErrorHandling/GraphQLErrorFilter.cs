namespace CentralServer.Presentation.GraphQL.ErrorHandling;

using CentralServer.Domain.Models;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.Hosting;

public sealed class GraphQLErrorFilter : IErrorFilter
{
    private readonly IHostEnvironment _environment;

    public GraphQLErrorFilter(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is DomainException)
        {
            return error
                .WithMessage(error.Exception.Message)
                .WithCode("DOMAIN_ERROR");
        }

        if (error.Exception is null)
        {
            return error;
        }

        var message = _environment.IsDevelopment()
            ? error.Exception.Message
            : "An unexpected error occurred while processing the GraphQL request.";

        return error
            .RemoveException()
            .WithMessage(message)
            .WithCode("UNEXPECTED_ERROR");
    }
}
