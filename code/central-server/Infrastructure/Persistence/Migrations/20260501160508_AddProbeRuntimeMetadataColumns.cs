using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProbeRuntimeMetadataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent_version",
                table: "probes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_metrics_push",
                table: "probes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_seen_at",
                table: "probes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ssid",
                table: "probes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agent_version",
                table: "probes");

            migrationBuilder.DropColumn(
                name: "last_metrics_push",
                table: "probes");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "probes");

            migrationBuilder.DropColumn(
                name: "ssid",
                table: "probes");
        }
    }
}
