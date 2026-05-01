using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProbePluginAssignmentsAndPluginMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bundle_download_url",
                table: "plugins",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dashboard_json",
                table: "plugins",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "probe_plugin_assignments",
                columns: table => new
                {
                    probe_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plugin_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_probe_plugin_assignments", x => new { x.probe_id, x.plugin_id });
                    table.ForeignKey(
                        name: "FK_probe_plugin_assignments_plugins_plugin_id",
                        column: x => x.plugin_id,
                        principalTable: "plugins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_probe_plugin_assignments_probes_probe_id",
                        column: x => x.probe_id,
                        principalTable: "probes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_plugins_bundle_download_url",
                table: "plugins",
                column: "bundle_download_url");

            migrationBuilder.CreateIndex(
                name: "idx_probe_plugin_plugin_id",
                table: "probe_plugin_assignments",
                column: "plugin_id");

            migrationBuilder.CreateIndex(
                name: "idx_probe_plugin_probe_id",
                table: "probe_plugin_assignments",
                column: "probe_id");

            migrationBuilder.Sql(
                """
                INSERT INTO probe_plugin_assignments (probe_id, plugin_id, assigned_at)
                SELECT probes.id, plugins.id, NOW()
                FROM probes
                CROSS JOIN plugins
                ON CONFLICT (probe_id, plugin_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "probe_plugin_assignments");

            migrationBuilder.DropIndex(
                name: "idx_plugins_bundle_download_url",
                table: "plugins");

            migrationBuilder.DropColumn(
                name: "bundle_download_url",
                table: "plugins");

            migrationBuilder.DropColumn(
                name: "dashboard_json",
                table: "plugins");
        }
    }
}
