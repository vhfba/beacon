namespace CentralServer.Infrastructure.Persistence.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
[Table("probe_test_configurations")]
public class ProbeTestConfigEntity
{
    [Column("probe_id")]
    [StringLength(50)]
    public string ProbeId { get; set; } = string.Empty;

    [Column("test_type")]
    [StringLength(50)]
    public string TestType { get; set; } = string.Empty;

    [Column("interval_seconds")]
    public int IntervalSeconds { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [ForeignKey(nameof(ProbeId))]
    public ProbeEntity? Probe { get; set; }

    [ForeignKey(nameof(TestType))]
    public TestTypeEntity? TestTypeEntity { get; set; }
}
