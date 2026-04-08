namespace CentralServer.Domain.Models;
public record TestType
{
    public string Name { get; }
    public string Description { get; }

    public TestType(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Test type name cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Test type description cannot be empty");

        if (name.Length > 50)
            throw new DomainException("Test type name cannot exceed 50 characters");

        Name = name;
        Description = description;
    }

    public override string ToString() => Name;
}
