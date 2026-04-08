namespace CentralServer.Domain.Models;
public class Probe
{
    public ProbeId Id { get; private set; }
    public string Name { get; private set; }
    public string Location { get; private set; }
    public string IpAddress { get; private set; }
    public ProbeStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastHeartbeat { get; private set; }
    public DateTime? LastConfigFetch { get; private set; }
    public long Version { get; private set; }

    public Probe(ProbeId id, string name, string location, string ipAddress)
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
        Status = ProbeStatus.Registered;
        CreatedAt = DateTime.UtcNow;
        Version = 0;
    }

    public static Probe Rehydrate(
        string id,
        string name,
        string location,
        string ipAddress,
        string status,
        DateTime createdAt,
        DateTime? lastHeartbeat,
        DateTime? lastConfigFetch,
        long version)
    {
        if (!Enum.TryParse<ProbeStatus>(status, true, out var parsedStatus))
            throw new DomainException($"Invalid probe status persisted in storage: {status}");

        var probe = new Probe(new ProbeId(id), name, location, ipAddress)
        {
            Status = parsedStatus,
            CreatedAt = createdAt,
            LastHeartbeat = lastHeartbeat,
            LastConfigFetch = lastConfigFetch,
            Version = version
        };

        return probe;
    }
    public void UpdateStatus(ProbeStatus newStatus)
    {
        Status = newStatus;
        Version++;
    }
    public void RecordHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
        if (Status == ProbeStatus.Registered)
            UpdateStatus(ProbeStatus.Active);
    }
    public void RecordConfigFetch()
    {
        LastConfigFetch = DateTime.UtcNow;
    }
}
