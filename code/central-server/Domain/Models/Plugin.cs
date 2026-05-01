namespace CentralServer.Domain.Models;
public class Plugin
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public string Version { get; private set; }
    public string Checksum { get; private set; }
    public string? Description { get; private set; }
    public string? BundleDownloadUrl { get; private set; }
    public string? DashboardJson { get; private set; }
    public DateTime ReleasedAt { get; private set; }
    public bool Available { get; private set; }
    public PluginExecutionMode ExecutionMode { get; private set; }

    public Plugin(
        string id,
        string name,
        string version,
        string checksum,
        string? description = null,
        string? bundleDownloadUrl = null,
        string? dashboardJson = null,
        PluginExecutionMode executionMode = PluginExecutionMode.Scheduled)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Plugin ID cannot be empty");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Plugin name cannot be empty");
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException("Plugin version cannot be empty");
        if (string.IsNullOrWhiteSpace(checksum))
            throw new DomainException("Plugin checksum cannot be empty");

        if (id.Length > 100)
            throw new DomainException("Plugin ID cannot exceed 100 characters");
        if (name.Length > 100)
            throw new DomainException("Plugin name cannot exceed 100 characters");
        if (version.Length > 50)
            throw new DomainException("Plugin version cannot exceed 50 characters");
        if (checksum.Length > 128)
            throw new DomainException("Plugin checksum cannot exceed 128 characters");
        if (!string.IsNullOrWhiteSpace(bundleDownloadUrl) &&
            !Uri.TryCreate(bundleDownloadUrl, UriKind.Absolute, out _))
            throw new DomainException("Bundle download URL must be a valid absolute URI");
        if (!Enum.IsDefined(executionMode))
            throw new DomainException("Plugin execution mode is invalid");

        Id = id;
        Name = name;
        Version = version;
        Checksum = checksum;
        Description = description;
        BundleDownloadUrl = string.IsNullOrWhiteSpace(bundleDownloadUrl) ? null : bundleDownloadUrl.Trim();
        DashboardJson = string.IsNullOrWhiteSpace(dashboardJson) ? null : dashboardJson.Trim();
        ReleasedAt = DateTime.UtcNow;
        Available = true;
        ExecutionMode = executionMode;
    }

    public static Plugin Rehydrate(
        string id,
        string name,
        string version,
        string checksum,
        string? description,
        string? bundleDownloadUrl,
        string? dashboardJson,
        DateTime releasedAt,
        bool available,
        PluginExecutionMode executionMode)
    {
        var plugin = new Plugin(id, name, version, checksum, description, bundleDownloadUrl, dashboardJson, executionMode)
        {
            ReleasedAt = releasedAt,
            Available = available
        };

        return plugin;
    }
    public void Retire()
    {
        Available = false;
    }
    public void Restore()
    {
        Available = true;
    }
}
