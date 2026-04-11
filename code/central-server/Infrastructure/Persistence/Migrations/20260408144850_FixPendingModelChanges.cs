using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_probe_test_config_probes",
                table: "probe_test_configurations");

            migrationBuilder.DropForeignKey(
                name: "fk_probe_test_config_test_types",
                table: "probe_test_configurations");

            migrationBuilder.DropIndex(
                name: "idx_probes_created_at",
                table: "probes");

            migrationBuilder.DropIndex(
                name: "idx_probes_last_heartbeat",
                table: "probes");

            migrationBuilder.DropIndex(
                name: "idx_plugins_released_at",
                table: "plugins");

            migrationBuilder.CreateIndex(
                name: "idx_probes_created_at",
                table: "probes",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_probes_last_heartbeat",
                table: "probes",
                column: "last_heartbeat",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_plugins_released_at",
                table: "plugins",
                column: "released_at",
                descending: new bool[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_probe_test_configurations_probes_probe_id",
                table: "probe_test_configurations",
                column: "probe_id",
                principalTable: "probes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_probe_test_configurations_test_types_test_type",
                table: "probe_test_configurations",
                column: "test_type",
                principalTable: "test_types",
                principalColumn: "name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_probe_test_configurations_probes_probe_id",
                table: "probe_test_configurations");

            migrationBuilder.DropForeignKey(
                name: "FK_probe_test_configurations_test_types_test_type",
                table: "probe_test_configurations");

            migrationBuilder.DropIndex(
                name: "idx_probes_created_at",
                table: "probes");

            migrationBuilder.DropIndex(
                name: "idx_probes_last_heartbeat",
                table: "probes");

            migrationBuilder.DropIndex(
                name: "idx_plugins_released_at",
                table: "plugins");

            migrationBuilder.CreateIndex(
                name: "idx_probes_created_at",
                table: "probes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_probes_last_heartbeat",
                table: "probes",
                column: "last_heartbeat");

            migrationBuilder.CreateIndex(
                name: "idx_plugins_released_at",
                table: "plugins",
                column: "released_at");

            migrationBuilder.AddForeignKey(
                name: "fk_probe_test_config_probes",
                table: "probe_test_configurations",
                column: "probe_id",
                principalTable: "probes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_probe_test_config_test_types",
                table: "probe_test_configurations",
                column: "test_type",
                principalTable: "test_types",
                principalColumn: "name");
        }
    }
}
