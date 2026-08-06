using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260806103000_AddNaturalEmotionalRegisters")]
public sealed class AddNaturalEmotionalRegisters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "natural_emotional_register",
            table: "run_combatants",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false);

        migrationBuilder.AddColumn<string>(
            name: "emotional_register_code",
            table: "run_character_snapshots",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "natural_emotional_register",
            table: "run_combatants");

        migrationBuilder.DropColumn(
            name: "emotional_register_code",
            table: "run_character_snapshots");
    }
}
