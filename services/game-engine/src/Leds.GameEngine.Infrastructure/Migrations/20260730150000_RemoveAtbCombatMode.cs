using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730150000_RemoveAtbCombatMode")]
public partial class RemoveAtbCombatMode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Une sauvegarde contenant encore un runtime ATB est incompatible avec le
        // contrat T-RPG canonique et doit être supprimée entièrement.
        migrationBuilder.Sql(
            """
            UPDATE runs
            SET active_combat_id = NULL
            WHERE LOWER(combat_mode) = 'atb';

            DELETE FROM run_active_combats
            WHERE LOWER(kind) = 'atb';

            DELETE FROM runs
            WHERE LOWER(combat_mode) = 'atb';
            """);

        migrationBuilder.DropColumn(
            name: "combat_mode",
            table: "runs");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "combat_mode",
            table: "runs",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Tactical");
    }
}
