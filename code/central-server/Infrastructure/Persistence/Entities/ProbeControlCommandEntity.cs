namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CentralServer.Domain.Models;

[Table("probe_control_commands")]
public class ProbeControlCommandEntity
{
    [Key]
    [Column("command_id")]
    [StringLength(32)]
    public string CommandId { get; set; } = string.Empty;

    [Column("probe_id")]
    [StringLength(50)]
    public string ProbeId { get; set; } = string.Empty;

    [Column("type")]
    public ProbeControlCommandType Type { get; set; }

    [Column("status")]
    public ProbeControlCommandStatus Status { get; set; }

    [Column("requested_by")]
    [StringLength(100)]
    public string RequestedBy { get; set; } = string.Empty;

    [Column("requested_at_utc")]
    public DateTime RequestedAtUtc { get; set; }

    [Column("delivered_at_utc")]
    public DateTime? DeliveredAtUtc { get; set; }

    [Column("started_at_utc")]
    public DateTime? StartedAtUtc { get; set; }

    [Column("completed_at_utc")]
    public DateTime? CompletedAtUtc { get; set; }

    [Column("payload_json")]
    public string? PayloadJson { get; set; }

    [Column("result_json")]
    public string? ResultJson { get; set; }

    [Column("error_message")]
    [StringLength(1024)]
    public string? ErrorMessage { get; set; }

    public ProbeEntity Probe { get; set; } = null!;
}
