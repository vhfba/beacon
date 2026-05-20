using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProbeControlCommands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "probe_control_commands",
                columns: table => new
                {
                    command_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    probe_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requested_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: true),
                    result_json = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_probe_control_commands", x => x.command_id);
                    table.ForeignKey(
                        name: "FK_probe_control_commands_probes_probe_id",
                        column: x => x.probe_id,
                        principalTable: "probes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_probe_control_probe_id",
                table: "probe_control_commands",
                column: "probe_id");

            migrationBuilder.CreateIndex(
                name: "idx_probe_control_probe_status_requested",
                table: "probe_control_commands",
                columns: new[] { "probe_id", "status", "requested_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_probe_control_status",
                table: "probe_control_commands",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "probe_control_commands");
        }
    }
}
