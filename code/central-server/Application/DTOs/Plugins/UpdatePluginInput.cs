namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using CentralServer.Domain.Models;

public record UpdatePluginInput
{
    [Required(ErrorMessage = "Current plugin ID is required")]
    [StringLength(100, ErrorMessage = "Current plugin ID cannot exceed 100 characters")]
    public string CurrentId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Plugin ID is required")]
    [StringLength(100, ErrorMessage = "Plugin ID cannot exceed 100 characters")]
    public string Id { get; init; } = string.Empty;

    [Required(ErrorMessage = "Plugin name is required")]
    [StringLength(100, ErrorMessage = "Plugin name cannot exceed 100 characters")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Plugin version is required")]
    [StringLength(50, ErrorMessage = "Plugin version cannot exceed 50 characters")]
    public string Version { get; init; } = string.Empty;

    [Required(ErrorMessage = "Plugin checksum is required")]
    [StringLength(128, ErrorMessage = "Plugin checksum cannot exceed 128 characters")]
    public string Checksum { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; init; }

    [StringLength(2048, ErrorMessage = "Bundle download URL cannot exceed 2048 characters")]
    [Url(ErrorMessage = "Bundle download URL must be a valid absolute URI")]
    public string? BundleDownloadUrl { get; init; }

    public string? DashboardJson { get; init; }

    public PluginExecutionMode ExecutionMode { get; init; } = PluginExecutionMode.Scheduled;
}
