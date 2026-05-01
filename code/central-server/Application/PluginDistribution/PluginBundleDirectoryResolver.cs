namespace CentralServer.Application.PluginDistribution;

using IOPath = System.IO.Path;

public static class PluginBundleDirectoryResolver
{
    public static string Resolve(string? configuredBundleDirectory, string contentRootPath)
    {
        var bundleDirectory = string.IsNullOrWhiteSpace(configuredBundleDirectory)
            ? IOPath.Combine(contentRootPath, PluginBundleConventions.DefaultBundleDirectory)
            : configuredBundleDirectory;

        if (!IOPath.IsPathRooted(bundleDirectory))
        {
            bundleDirectory = IOPath.GetFullPath(IOPath.Combine(contentRootPath, bundleDirectory));
        }

        Directory.CreateDirectory(bundleDirectory);
        return bundleDirectory;
    }
}
