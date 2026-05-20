namespace CentralServer.Domain.Models;
public record ProbeTestConfiguration
{
    public ProbeId ProbeId { get; }
    public string PluginId { get; }
    public int IntervalSeconds { get; }
    public bool Enabled { get; }

    public ProbeTestConfiguration(ProbeId probeId, string pluginId, int intervalSeconds, bool enabled = true)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new DomainException("Plugin id cannot be empty");

        if (pluginId.Length > 50)
            throw new DomainException("Plugin id cannot exceed 50 characters");

        if (intervalSeconds < 5 || intervalSeconds > 3600)
            throw new DomainException("Interval seconds must be between 5 and 3600");

        ProbeId = probeId;
        PluginId = pluginId;
        IntervalSeconds = intervalSeconds;
        Enabled = enabled;
    }

    public ProbeTestConfiguration WithInterval(int newIntervalSeconds)
        => new(ProbeId, PluginId, newIntervalSeconds, Enabled);

    public ProbeTestConfiguration WithEnabled(bool newEnabled)
        => new(ProbeId, PluginId, IntervalSeconds, newEnabled);
}
