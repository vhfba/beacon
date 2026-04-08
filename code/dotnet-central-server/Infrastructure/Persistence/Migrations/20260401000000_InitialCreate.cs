using CentralServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CentralServer.Infrastructure.Persistence.Migrations;

#nullable disable

[DbContext(typeof(CentralServerDbContext))]
[Migration("20260401000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
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
        migrationBuilder.CreateTable(
            name: "probes",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_heartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                last_config_fetch = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_probes", x => x.id);
                table.UniqueConstraint("unique_ip_probe", x => x.ip_address);
            });
        migrationBuilder.CreateIndex(
            name: "idx_probes_status",
            table: "probes",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "idx_probes_created_at",
            table: "probes",
            column: "created_at",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "idx_probes_last_heartbeat",
            table: "probes",
            column: "last_heartbeat",
            descending: new[] { true });
        migrationBuilder.CreateTable(
            name: "probe_test_configurations",
            columns: table => new
            {
                probe_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                test_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                interval_seconds = table.Column<int>(type: "integer", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_probe_test_config", x => new { x.probe_id, x.test_type });
                table.ForeignKey(
                    name: "fk_probe_test_config_probes",
                    column: x => x.probe_id,
                    principalTable: "probes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_probe_test_config_test_types",
                    column: x => x.test_type,
                    principalTable: "test_types",
                    principalColumn: "name",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "idx_probe_config_probe_id",
            table: "probe_test_configurations",
            column: "probe_id");

        migrationBuilder.CreateIndex(
            name: "idx_probe_config_enabled",
            table: "probe_test_configurations",
            column: "enabled");

        migrationBuilder.CreateIndex(
            name: "idx_probe_config_probe_enabled",
            table: "probe_test_configurations",
            columns: new[] { "probe_id", "enabled" });
        migrationBuilder.CreateTable(
            name: "plugins",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                available = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_plugins", x => x.id);
            });
        migrationBuilder.CreateIndex(
            name: "idx_plugins_name",
            table: "plugins",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "idx_plugins_name_version",
            table: "plugins",
            columns: new[] { "name", "version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_plugins_available",
            table: "plugins",
            column: "available");

        migrationBuilder.CreateIndex(
            name: "idx_plugins_released_at",
            table: "plugins",
            column: "released_at",
            descending: new[] { true });
        migrationBuilder.InsertData(
            table: "test_types",
            columns: new[] { "name", "description" },
            values: new object[,]
            {
                { "RSSI", "Receive Signal Strength Indicator measurement" },
                { "PING", "ICMP echo request to measure latency" },
                { "HTTP", "HTTP connectivity and response time test" },
                { "IPERF", "Network throughput measurement" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "probe_test_configurations");
        migrationBuilder.DropTable(name: "plugins");
        migrationBuilder.DropTable(name: "probes");
        migrationBuilder.DropTable(name: "test_types");
    }
}
