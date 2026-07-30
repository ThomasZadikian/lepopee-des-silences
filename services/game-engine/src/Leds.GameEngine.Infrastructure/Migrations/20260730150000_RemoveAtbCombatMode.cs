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
        // Une sauvegarde ATB ne peut pas être convertie sans réinterpréter son tour courant.
        // On la clôt donc explicitement avant de supprimer son runtime et le sélecteur de mode.
        migrationBuilder.Sql(
            """
            UPDATE runs
            SET status = 'Abandoned',
                ended_at_utc = COALESCE(ended_at_utc, CURRENT_TIMESTAMP),
                active_combat_id = NULL,
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE LOWER(combat_mode) = 'atb'
              AND status NOT IN ('Completed', 'Failed', 'Abandoned');

            DELETE FROM run_active_combats
            WHERE LOWER(kind) = 'atb';
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
