namespace CentralServer.Application.Monitoring;

public static class GrafanaDashboardConventions
{
    public static string BuildPluginDashboardUid(string pluginId)
    {
        var slug = new string((pluginId ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ToSlugCharacter)
            .ToArray())
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "plugin";
        }

        var uid = $"beacon-plugin-{slug}";
        return uid.Length <= 40 ? uid : uid[..40];
    }

    public static string BuildDashboardApiUrl(string apiBaseUrl)
    {
        return CombineUrl(apiBaseUrl, "/api/dashboards/db");
    }

    public static string BuildDashboardSearchApiUrl(string apiBaseUrl)
    {
        return CombineUrl(apiBaseUrl, "/api/search?type=dash-db");
    }

    public static string BuildDashboardDeleteApiUrl(string apiBaseUrl, string dashboardUid)
    {
        return CombineUrl(apiBaseUrl, $"/api/dashboards/uid/{Uri.EscapeDataString(dashboardUid)}");
    }

    public static string BuildEmbedUrl(string embedBaseUrl, string dashboardUid, string site)
    {
        var encodedSite = Uri.EscapeDataString(string.IsNullOrWhiteSpace(site) ? "default" : site.Trim());
        return $"{embedBaseUrl.TrimEnd('/')}/d/{Uri.EscapeDataString(dashboardUid)}?kiosk&theme=light&var-site={encodedSite}";
    }

    private static char ToSlugCharacter(char ch)
    {
        return (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') ? ch : '-';
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
