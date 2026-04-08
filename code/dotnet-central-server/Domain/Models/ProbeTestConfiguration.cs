namespace CentralServer.Domain.Models;
public record ProbeTestConfiguration
{
    public ProbeId ProbeId { get; }
    public TestType TestType { get; }
    public int IntervalSeconds { get; }
    public bool Enabled { get; }

    public ProbeTestConfiguration(ProbeId probeId, TestType testType, int intervalSeconds, bool enabled = true)
    {
        if (intervalSeconds < 5 || intervalSeconds > 3600)
            throw new DomainException("Interval seconds must be between 5 and 3600");

        ProbeId = probeId;
        TestType = testType;
        IntervalSeconds = intervalSeconds;
        Enabled = enabled;
    }

    public ProbeTestConfiguration WithInterval(int newIntervalSeconds)
        => new(ProbeId, TestType, newIntervalSeconds, Enabled);

    public ProbeTestConfiguration WithEnabled(bool newEnabled)
        => new(ProbeId, TestType, IntervalSeconds, newEnabled);
}
