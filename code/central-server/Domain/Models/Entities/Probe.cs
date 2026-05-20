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
        EnsureReportedDetailsArePresent(name, location, ipAddress);

        Id = id;
        Name = name;
        Location = location;
        IpAddress = ipAddress;
        Ssid = NormalizeOptional(ssid);
        AgentVersion = NormalizeOptional(agentVersion);
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
        EnsureReportedDetailsArePresent(name, location, ipAddress);

        Name = name.Trim();
        Location = location.Trim();
        IpAddress = ipAddress.Trim();
        Ssid = NormalizeOptional(ssid);
        AgentVersion = NormalizeOptional(agentVersion);
        Version++;
    }

    public void UpdateObservedDetails(string ipAddress, string? ssid, string? agentVersion)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("IP address cannot be empty");

        IpAddress = ipAddress.Trim();
        Ssid = NormalizeOptional(ssid);
        AgentVersion = NormalizeOptional(agentVersion);
        Version++;
    }

    public void UpdateProfile(string name, string location)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Probe name cannot be empty");
        if (string.IsNullOrWhiteSpace(location))
            throw new DomainException("Probe location cannot be empty");

        Name = name.Trim();
        Location = location.Trim();
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

    private static void EnsureReportedDetailsArePresent(string name, string location, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Probe name cannot be empty");
        if (string.IsNullOrWhiteSpace(location))
            throw new DomainException("Probe location cannot be empty");
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("IP address cannot be empty");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
