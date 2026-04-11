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

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "Registered";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_heartbeat")]
    public DateTime? LastHeartbeat { get; set; }

    [Column("last_config_fetch")]
    public DateTime? LastConfigFetch { get; set; }

    [Column("version")]
    public long Version { get; set; }

    public ICollection<ProbeTestConfigEntity> TestConfigurations { get; set; } = new List<ProbeTestConfigEntity>();
}
