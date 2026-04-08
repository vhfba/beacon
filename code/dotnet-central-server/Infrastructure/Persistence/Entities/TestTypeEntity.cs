namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
[Table("test_types")]
public class TestTypeEntity
{
    [Key]
    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public ICollection<ProbeTestConfigEntity> ProbeConfigurations { get; set; } = new List<ProbeTestConfigEntity>();
}
