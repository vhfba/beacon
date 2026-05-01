namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CentralServer.Domain.Models;

[Table("probe_action_executions")]
public class ProbeActionExecutionEntity
{
    [Key]
    [Column("execution_id")]
    [StringLength(32)]
    public string ExecutionId { get; set; } = string.Empty;

    [Column("probe_id")]
    [StringLength(50)]
    public string ProbeId { get; set; } = string.Empty;

    [Column("plugin_id")]
    [StringLength(100)]
    public string PluginId { get; set; } = string.Empty;

    [Column("triggered_by")]
    [StringLength(100)]
    public string TriggeredBy { get; set; } = string.Empty;

    [Column("status")]
    public ProbeActionExecutionStatus Status { get; set; }

    [Column("requested_at_utc")]
    public DateTime RequestedAtUtc { get; set; }

    [Column("delivered_at_utc")]
    public DateTime? DeliveredAtUtc { get; set; }

    [Column("started_at_utc")]
    public DateTime? StartedAtUtc { get; set; }

    [Column("completed_at_utc")]
    public DateTime? CompletedAtUtc { get; set; }

    [Column("error_message")]
    [StringLength(1024)]
    public string? ErrorMessage { get; set; }

    public ProbeEntity Probe { get; set; } = null!;
    public PluginEntity Plugin { get; set; } = null!;
}
