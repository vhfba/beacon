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
        EnsureRequiredText(id, "Plugin ID cannot be empty");
        EnsureRequiredText(name, "Plugin name cannot be empty");
        EnsureRequiredText(version, "Plugin version cannot be empty");
        EnsureRequiredText(checksum, "Plugin checksum cannot be empty");
        EnsureMaxLength(id, 100, "Plugin ID cannot exceed 100 characters");
        EnsureMaxLength(name, 100, "Plugin name cannot exceed 100 characters");
        EnsureMaxLength(version, 50, "Plugin version cannot exceed 50 characters");
        EnsureMaxLength(checksum, 128, "Plugin checksum cannot exceed 128 characters");
        EnsureValidBundleDownloadUrl(bundleDownloadUrl);
        EnsureValidExecutionMode(executionMode);

        Id = id;
        Name = name;
        Version = version;
        Checksum = checksum;
        Description = description;
        BundleDownloadUrl = NormalizeOptional(bundleDownloadUrl);
        DashboardJson = NormalizeOptional(dashboardJson);
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

    public Plugin WithCatalogDetails(
        string id,
        string name,
        string version,
        string checksum,
        string? description,
        string? bundleDownloadUrl,
        string? dashboardJson,
        PluginExecutionMode executionMode)
    {
        return Rehydrate(
            id,
            name,
            version,
            checksum,
            description,
            bundleDownloadUrl,
            dashboardJson,
            ReleasedAt,
            Available,
            executionMode);
    }

    private static void EnsureRequiredText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(message);
    }

    private static void EnsureMaxLength(string value, int maxLength, string message)
    {
        if (value.Length > maxLength)
            throw new DomainException(message);
    }

    private static void EnsureValidBundleDownloadUrl(string? bundleDownloadUrl)
    {
        if (!string.IsNullOrWhiteSpace(bundleDownloadUrl) &&
            !Uri.TryCreate(bundleDownloadUrl, UriKind.Absolute, out _))
            throw new DomainException("Bundle download URL must be a valid absolute URI");
    }

    private static void EnsureValidExecutionMode(PluginExecutionMode executionMode)
    {
        if (!Enum.IsDefined(executionMode))
            throw new DomainException("Plugin execution mode is invalid");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
