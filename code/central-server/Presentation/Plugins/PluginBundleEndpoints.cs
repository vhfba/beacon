namespace CentralServer.Presentation.Plugins;

using CentralServer.Application.PluginDistribution;
using CentralServer.Domain.Repositories;
using CentralServer.Presentation.Security;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        endpoints.MapPost("/plugins/{pluginId}/{version}/bundle", async (
            string pluginId,
            string version,
            [FromForm] IFormFile bundle,
            CancellationToken cancellationToken) =>
        {
            if (!PluginBundleConventions.IsSafeSegment(pluginId) || !PluginBundleConventions.IsSafeSegment(version))
            {
                return Results.BadRequest(new { message = "Invalid plugin ID or version format." });
            }

            if (bundle == null || bundle.Length == 0)
            {
                return Results.BadRequest(new { message = "Upload a non-empty zip bundle." });
            }

            if (!bundle.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Plugin bundle must be a .zip file." });
            }

            var bundleFileName = PluginBundleConventions.BuildBundleFileName(pluginId, version);
            var bundlePath = IOPath.Combine(bundleDirectory, bundleFileName);
            Directory.CreateDirectory(bundleDirectory);

            await using (var stream = File.Create(bundlePath))
            {
                await bundle.CopyToAsync(stream, cancellationToken);
            }

            string checksum;
            await using (var readStream = File.OpenRead(bundlePath))
            {
                var hash = await SHA256.HashDataAsync(readStream, cancellationToken);
                checksum = Convert.ToHexString(hash).ToLowerInvariant();
            }

            return Results.Ok(new
            {
                message = "Plugin bundle uploaded.",
                pluginId,
                version,
                fileName = bundleFileName,
                size = new FileInfo(bundlePath).Length,
                checksum,
                bundleUrl = PluginBundleConventions.BuildBundleUrl(pluginId, version)
            });
        })
            .WithName("UploadPluginBundle")
            .DisableAntiforgery()
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return endpoints;
    }
}
