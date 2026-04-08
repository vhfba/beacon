namespace CentralServer.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record RegisterPluginInput
{
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
}
