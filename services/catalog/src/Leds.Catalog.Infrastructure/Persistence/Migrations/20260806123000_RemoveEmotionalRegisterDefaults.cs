using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("20260806123000_RemoveEmotionalRegisterDefaults")]
public sealed class RemoveEmotionalRegisterDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        Alter(migrationBuilder, "catalog_npc_definitions", "emotional_affinity", false);
        Alter(migrationBuilder, "catalog_skill_definitions", "emotional_register", false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        Alter(migrationBuilder, "catalog_npc_definitions", "emotional_affinity", true);
        Alter(migrationBuilder, "catalog_skill_definitions", "emotional_register", true);
    }

    private static void Alter(MigrationBuilder migrationBuilder, string table, string column, bool restoreDefault)
    {
        if (restoreDefault)
        {
            migrationBuilder.AlterColumn<string>(
                name: column, table: table, type: "character varying(32)", maxLength: 32,
                nullable: false, defaultValue: "Neutral",
                oldClrType: typeof(string), oldType: "character varying(32)", oldMaxLength: 32);
            return;
        }

        migrationBuilder.AlterColumn<string>(
            name: column, table: table, type: "character varying(32)", maxLength: 32,
            nullable: false,
            oldClrType: typeof(string), oldType: "character varying(32)", oldMaxLength: 32,
            oldDefaultValue: "Neutral");
    }
}
