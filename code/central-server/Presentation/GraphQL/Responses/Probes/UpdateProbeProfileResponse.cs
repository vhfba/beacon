namespace CentralServer.Presentation.GraphQL.Responses;

using CentralServer.Presentation.GraphQL.Types;

public class UpdateProbeProfileResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ProbeType? Probe { get; set; }
    public ProbeControlCommandType? Command { get; set; }
}
