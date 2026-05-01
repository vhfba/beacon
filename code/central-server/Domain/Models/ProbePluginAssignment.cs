namespace CentralServer.Domain.Models;

public sealed class ProbePluginAssignment
{
    public ProbeId ProbeId { get; private set; }
    public string PluginId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public ProbePluginAssignment(ProbeId probeId, string pluginId, DateTime? assignedAt = null)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new DomainException("Plugin ID cannot be empty");

        ProbeId = probeId;
        PluginId = pluginId;
        AssignedAt = assignedAt ?? DateTime.UtcNow;
    }

    public static ProbePluginAssignment Rehydrate(string probeId, string pluginId, DateTime assignedAt)
    {
        return new ProbePluginAssignment(new ProbeId(probeId), pluginId, assignedAt);
    }
}
