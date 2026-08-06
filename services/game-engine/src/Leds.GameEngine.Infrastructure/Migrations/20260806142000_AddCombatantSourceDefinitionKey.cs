using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260806142000_AddCombatantSourceDefinitionKey")]
public sealed class AddCombatantSourceDefinitionKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "source_definition_key",
            table: "run_combatants",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "source_definition_key", table: "run_combatants");
}
