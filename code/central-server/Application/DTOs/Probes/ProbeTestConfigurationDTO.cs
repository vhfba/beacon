namespace CentralServer.Application.DTOs;
public record ProbeTestConfigurationDTO
{
    public string ProbeId { get; init; } = string.Empty;
    public string TestType { get; init; } = string.Empty;
    public int IntervalSeconds { get; init; }
    public bool Enabled { get; init; }
}
