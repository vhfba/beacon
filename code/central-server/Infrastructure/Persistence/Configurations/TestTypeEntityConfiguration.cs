namespace CentralServer.Infrastructure.Persistence.Configurations;

using CentralServer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class TestTypeEntityConfiguration : IEntityTypeConfiguration<TestTypeEntity>
{
    public void Configure(EntityTypeBuilder<TestTypeEntity> builder)
    {
        builder.HasData(
            new TestTypeEntity { Name = "RSSI", Description = "Receive Signal Strength Indicator measurement" },
            new TestTypeEntity { Name = "PING", Description = "ICMP echo request to measure latency" },
            new TestTypeEntity { Name = "HTTP", Description = "HTTP connectivity and response time test" },
            new TestTypeEntity { Name = "IPERF", Description = "Network throughput measurement" });
    }
}
