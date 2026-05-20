namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
[Table("probes")]
public class ProbeEntity
{
    [Key]
    [Column("id")]
    [StringLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("location")]
    [StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Column("ip_address")]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [Column("ssid")]
    [StringLength(128)]
    public string? Ssid { get; set; }

    [Column("agent_version")]
    [StringLength(100)]
    public string? AgentVersion { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "Registered";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_heartbeat")]
    public DateTime? LastHeartbeat { get; set; }

    [Column("last_config_fetch")]
    public DateTime? LastConfigFetch { get; set; }

    [Column("last_metrics_push")]
    public DateTime? LastMetricsPush { get; set; }

    [Column("last_seen_at")]
    public DateTime? LastSeenAt { get; set; }

    [Column("version")]
    public long Version { get; set; }

    public ICollection<ProbeTestConfigEntity> TestConfigurations { get; set; } = new List<ProbeTestConfigEntity>();

    public ICollection<ProbePluginAssignmentEntity> PluginAssignments { get; set; } = new List<ProbePluginAssignmentEntity>();

    public ICollection<ProbeControlCommandEntity> ControlCommands { get; set; } = new List<ProbeControlCommandEntity>();
}
