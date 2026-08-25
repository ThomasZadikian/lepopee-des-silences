using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260824152000_PersistPartyResources")]
public partial class PersistPartyResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "current_vitality",
            table: "run_character_snapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "current_mana",
            table: "run_character_snapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE run_character_snapshots AS character
            SET current_vitality = stats.max_vitality,
                current_mana = stats.mana
            FROM run_character_stat_snapshots AS stats
            WHERE stats.character_snapshot_id = character.id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "current_vitality", table: "run_character_snapshots");
        migrationBuilder.DropColumn(name: "current_mana", table: "run_character_snapshots");
    }
}
