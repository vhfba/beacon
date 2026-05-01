namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CentralServer.Domain.Models;
[Table("plugins")]
public class PluginEntity
{
    [Key]
    [Column("id")]
    [StringLength(100)]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("version")]
    [StringLength(50)]
    public string Version { get; set; } = string.Empty;

    [Column("checksum")]
    [StringLength(128)]
    public string Checksum { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Column("bundle_download_url")]
    [StringLength(2048)]
    public string? BundleDownloadUrl { get; set; }

    [Column("dashboard_json")]
    public string? DashboardJson { get; set; }

    [Column("released_at")]
    public DateTime ReleasedAt { get; set; }

    [Column("available")]
    public bool Available { get; set; }

    [Column("execution_mode")]
    public PluginExecutionMode ExecutionMode { get; set; } = PluginExecutionMode.Scheduled;

    public ICollection<ProbePluginAssignmentEntity> ProbeAssignments { get; set; } = new List<ProbePluginAssignmentEntity>();
}
