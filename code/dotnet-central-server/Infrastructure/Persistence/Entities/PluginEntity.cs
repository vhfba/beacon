namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    [Column("released_at")]
    public DateTime ReleasedAt { get; set; }

    [Column("available")]
    public bool Available { get; set; }
}
