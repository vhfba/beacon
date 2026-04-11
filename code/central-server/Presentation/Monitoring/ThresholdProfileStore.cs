using System.Collections.Concurrent;

namespace CentralServer.Presentation.Monitoring;

public sealed class ThresholdProfileStore
{
    private readonly ConcurrentDictionary<string, WifiThresholdProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public WifiThresholdProfile Get(string site)
    {
        var key = NormalizeSite(site);
        return _profiles.TryGetValue(key, out var profile)
            ? profile
            : WifiThresholdProfile.Default();
    }

    public WifiThresholdProfile Set(string site, WifiThresholdProfile profile)
    {
        var key = NormalizeSite(site);
        _profiles[key] = profile;
        return profile;
    }

    public static string NormalizeSite(string? site)
    {
        return string.IsNullOrWhiteSpace(site)
            ? "default"
            : site.Trim();
    }
}
