namespace CentralServer.Application.PluginDistribution;

using System.Text.RegularExpressions;

public static class PluginBundleConventions
{
    public const string DefaultBundleDirectory = "plugin-bundles";

    private static readonly Regex SafeSegmentRegex = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    public static bool IsSafeSegment(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && SafeSegmentRegex.IsMatch(value);
    }

    public static string BuildBundleFileName(string pluginId, string version)
    {
        return $"{pluginId}-{version}.zip";
    }

    public static string BuildBundleUrl(string pluginId, string version)
    {
        return $"/plugins/{Uri.EscapeDataString(pluginId)}/{Uri.EscapeDataString(version)}/bundle";
    }
}
