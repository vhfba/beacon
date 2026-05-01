using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProbeActionsAndPluginExecutionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "execution_mode",
                table: "plugins",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "probe_action_executions",
                columns: table => new
                {
                    execution_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    probe_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plugin_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_probe_action_executions", x => x.execution_id);
                    table.ForeignKey(
                        name: "FK_probe_action_executions_plugins_plugin_id",
                        column: x => x.plugin_id,
                        principalTable: "plugins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_probe_action_executions_probes_probe_id",
                        column: x => x.probe_id,
                        principalTable: "probes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_probe_action_probe_id",
                table: "probe_action_executions",
                column: "probe_id");

            migrationBuilder.CreateIndex(
                name: "idx_probe_action_probe_status_requested",
                table: "probe_action_executions",
                columns: new[] { "probe_id", "status", "requested_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_probe_action_status",
                table: "probe_action_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_probe_action_executions_plugin_id",
                table: "probe_action_executions",
                column: "plugin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "probe_action_executions");

            migrationBuilder.DropColumn(
                name: "execution_mode",
                table: "plugins");
        }
    }
}
