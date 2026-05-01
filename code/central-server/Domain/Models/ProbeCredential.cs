namespace CentralServer.Domain.Models;

public class ProbeCredential
{
    public ProbeId ProbeId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    public ProbeCredential(ProbeId probeId, string tokenHash)
    {
        if (probeId is null)
            throw new DomainException("ProbeId is required");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash is required");

        ProbeId = probeId;
        TokenHash = tokenHash.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public static ProbeCredential Rehydrate(
        ProbeId probeId,
        string tokenHash,
        DateTime createdAt,
        DateTime? lastUsedAt)
    {
        var credential = new ProbeCredential(probeId, tokenHash)
        {
            CreatedAt = createdAt,
            LastUsedAt = lastUsedAt
        };

        return credential;
    }

    public void RotateToken(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash is required");

        TokenHash = tokenHash.Trim();
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = null;
    }

    public void Touch(DateTime usedAt)
    {
        LastUsedAt = usedAt;
    }
}
