using CentralServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CentralServer.Infrastructure.Persistence.Migrations;

#nullable disable

[DbContext(typeof(CentralServerDbContext))]
[Migration("20260518163000_RemoveProbeConfigTestTypeCatalogConstraint")]
public partial class RemoveProbeConfigTestTypeCatalogConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            DECLARE
                constraint_name text;
            BEGIN
                FOR constraint_name IN
                    SELECT con.conname
                    FROM pg_constraint con
                    JOIN pg_class rel ON rel.oid = con.conrelid
                    JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
                    WHERE nsp.nspname = 'public'
                      AND rel.relname = 'probe_test_configurations'
                      AND con.contype = 'f'
                      AND pg_get_constraintdef(con.oid) LIKE '%REFERENCES test_types%'
                LOOP
                    EXECUTE format('ALTER TABLE public.probe_test_configurations DROP CONSTRAINT %I', constraint_name);
                END LOOP;
            END $$;
            """);

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS public."IX_probe_test_configurations_test_type";
            DROP INDEX IF EXISTS public.ix_probe_test_configurations_test_type;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM probe_test_configurations pc
            WHERE NOT EXISTS (
                SELECT 1
                FROM test_types tt
                WHERE tt.name = pc.test_type
            );
            """);

        migrationBuilder.CreateIndex(
            name: "IX_probe_test_configurations_test_type",
            table: "probe_test_configurations",
            column: "test_type");

        migrationBuilder.AddForeignKey(
            name: "FK_probe_test_configurations_test_types_test_type",
            table: "probe_test_configurations",
            column: "test_type",
            principalTable: "test_types",
            principalColumn: "name",
            onDelete: ReferentialAction.Cascade);
    }
}
