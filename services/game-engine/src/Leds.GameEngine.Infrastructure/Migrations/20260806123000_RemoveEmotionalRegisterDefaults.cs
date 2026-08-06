using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260806123000_RemoveEmotionalRegisterDefaults")]
public sealed class RemoveEmotionalRegisterDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        DropDefault(migrationBuilder, "run_combatant_skills");
        DropDefault(migrationBuilder, "run_player_skills");
        DropDefault(migrationBuilder, "run_character_skill_snapshots");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RestoreDefault(migrationBuilder, "run_combatant_skills");
        RestoreDefault(migrationBuilder, "run_player_skills");
        RestoreDefault(migrationBuilder, "run_character_skill_snapshots");
    }

    private static void DropDefault(MigrationBuilder migrationBuilder, string table) =>
        migrationBuilder.AlterColumn<string>(
            name: "emotional_register",
            table: table,
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32,
            oldDefaultValue: "Neutral");

    private static void RestoreDefault(MigrationBuilder migrationBuilder, string table) =>
        migrationBuilder.AlterColumn<string>(
            name: "emotional_register",
            table: table,
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Neutral",
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32);
}
