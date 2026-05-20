using CentralServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CentralServer.Infrastructure.Persistence.Migrations;

#nullable disable

[DbContext(typeof(CentralServerDbContext))]
[Migration("20260519120000_DropTestTypesTable")]
public partial class DropTestTypesTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "test_types");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "test_types",
            columns: table => new
            {
                name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_test_types", x => x.name);
            });

        migrationBuilder.InsertData(
            table: "test_types",
            columns: new[] { "name", "description" },
            columnTypes: new[] { "character varying(50)", "character varying(500)" },
            values: new object[,]
            {
                { "RSSI", "Receive Signal Strength Indicator measurement" },
                { "PING", "ICMP echo request to measure latency" },
                { "HTTP", "HTTP connectivity and response time test" },
                { "IPERF", "Network throughput measurement" }
            });
    }
}
