namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;

public class ProbeControlCommandResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ProbeControlCommandType? Command { get; set; }
}
