namespace CentralServer.Domain.Models;
public class Probe
{
    public ProbeId Id { get; private set; }
    public string Name { get; private set; }
    public string Location { get; private set; }
    public string IpAddress { get; private set; }
    public string? Ssid { get; private set; }
    public string? AgentVersion { get; private set; }
    public ProbeStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastHeartbeat { get; private set; }
    public DateTime? LastConfigFetch { get; private set; }
    public DateTime? LastMetricsPush { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public long Version { get; private set; }

    public Probe(ProbeId id, string name, string location, string ipAddress, string? ssid = null, string? agentVersion = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Probe name cannot be empty");
        if (string.IsNullOrWhiteSpace(location))
            throw new DomainException("Probe location cannot be empty");
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("IP address cannot be empty");

        Id = id;
        Name = name;
        Location = location;
        IpAddress = ipAddress;
        Ssid = string.IsNullOrWhiteSpace(ssid) ? null : ssid.Trim();
        AgentVersion = string.IsNullOrWhiteSpace(agentVersion) ? null : agentVersion.Trim();
        Status = ProbeStatus.Registered;
        CreatedAt = DateTime.UtcNow;
        Version = 0;
    }

    public static Probe Rehydrate(
        string id,
        string name,
        string location,
        string ipAddress,
        string? ssid,
        string? agentVersion,
        string status,
        DateTime createdAt,
        DateTime? lastHeartbeat,
        DateTime? lastConfigFetch,
        DateTime? lastMetricsPush,
        DateTime? lastSeenAt,
        long version)
    {
        if (!Enum.TryParse<ProbeStatus>(status, true, out var parsedStatus))
            throw new DomainException($"Invalid probe status persisted in storage: {status}");

        var probe = new Probe(new ProbeId(id), name, location, ipAddress, ssid, agentVersion)
        {
            Status = parsedStatus,
            CreatedAt = createdAt,
            LastHeartbeat = lastHeartbeat,
            LastConfigFetch = lastConfigFetch,
            LastMetricsPush = lastMetricsPush,
            LastSeenAt = lastSeenAt,
            Version = version
        };

        return probe;
    }

    public void UpdateReportedDetails(string name, string location, string ipAddress, string? ssid, string? agentVersion)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Probe name cannot be empty");
        if (string.IsNullOrWhiteSpace(location))
            throw new DomainException("Probe location cannot be empty");
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("IP address cannot be empty");

        Name = name.Trim();
        Location = location.Trim();
        IpAddress = ipAddress.Trim();
        Ssid = string.IsNullOrWhiteSpace(ssid) ? null : ssid.Trim();
        AgentVersion = string.IsNullOrWhiteSpace(agentVersion) ? null : agentVersion.Trim();
        Version++;
    }
    public void UpdateStatus(ProbeStatus newStatus)
    {
        Status = newStatus;
        Version++;
    }
    public void RecordHeartbeatAndActivate()
    {
        LastHeartbeat = DateTime.UtcNow;
        LastSeenAt = LastHeartbeat;
        if (Status != ProbeStatus.Active && Status != ProbeStatus.Decommissioned)
            UpdateStatus(ProbeStatus.Active);
    }

    public void RecordPassiveHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
        LastSeenAt = LastHeartbeat;
    }

    public void RecordMetricsPush()
    {
        LastMetricsPush = DateTime.UtcNow;
        LastSeenAt = LastMetricsPush;
        if (Status != ProbeStatus.Active && Status != ProbeStatus.Decommissioned)
            UpdateStatus(ProbeStatus.Active);
    }
    public void RecordConfigFetch()
    {
        LastConfigFetch = DateTime.UtcNow;
    }
}
