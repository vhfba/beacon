namespace CentralServer.Presentation.Plugins;

using CentralServer.Application.PluginDistribution;
using CentralServer.Domain.Repositories;
using CentralServer.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using IOPath = System.IO.Path;

public static class PluginBundleEndpoints
{
    public static IEndpointRouteBuilder MapPluginBundleEndpoints(this IEndpointRouteBuilder endpoints, string bundleDirectory)
    {
        endpoints.MapGet("/plugins/{pluginId}/{version}/bundle", async (
            string pluginId,
            string version,
            IPluginRepository pluginRepository,
            CancellationToken cancellationToken) =>
        {
            if (!PluginBundleConventions.IsSafeSegment(pluginId) || !PluginBundleConventions.IsSafeSegment(version))
            {
                return Results.BadRequest(new { message = "Invalid plugin ID or version format." });
            }

            var plugin = await pluginRepository.GetByIdAsync(pluginId, cancellationToken);
            if (plugin == null || !plugin.Available || !string.Equals(plugin.Version, version, StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound(new { message = "Plugin bundle not found." });
            }

            var bundleFileName = PluginBundleConventions.BuildBundleFileName(plugin.Id, plugin.Version);
            var bundlePath = IOPath.Combine(bundleDirectory, bundleFileName);
            if (!File.Exists(bundlePath))
            {
                return Results.NotFound(new { message = "Plugin bundle not found." });
            }

            return Results.File(
                bundlePath,
                contentType: "application/zip",
                fileDownloadName: bundleFileName,
                enableRangeProcessing: true);
        })
            .WithName("DownloadPluginBundle")
            .RequireAuthorization(AuthorizationPolicies.ProbeOrAdmin);

        return endpoints;
    }
}
