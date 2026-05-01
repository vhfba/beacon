namespace CentralServer.Application.DTOs;
public record ProbeConfigDTO
{
    public string ProbeId { get; init; } = string.Empty;
    public List<ProbeTestConfigurationDTO> EnabledTests { get; init; } = [];
    public List<PluginDTO> AvailablePlugins { get; init; } = [];
}
