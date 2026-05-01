namespace CentralServer.Domain.Models;
public record ProbeId
{
    public string Value { get; }

    public ProbeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("ProbeId cannot be empty");

        if (value.Length > 50)
            throw new DomainException("ProbeId cannot exceed 50 characters");

        Value = value;
    }

    public override string ToString() => Value;
}
