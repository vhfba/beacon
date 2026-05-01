namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("probe_plugin_assignments")]
public class ProbePluginAssignmentEntity
{
    [Column("probe_id")]
    [StringLength(50)]
    public string ProbeId { get; set; } = string.Empty;

    [Column("plugin_id")]
    [StringLength(100)]
    public string PluginId { get; set; } = string.Empty;

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [ForeignKey(nameof(ProbeId))]
    public ProbeEntity? Probe { get; set; }

    [ForeignKey(nameof(PluginId))]
    public PluginEntity? Plugin { get; set; }
}
