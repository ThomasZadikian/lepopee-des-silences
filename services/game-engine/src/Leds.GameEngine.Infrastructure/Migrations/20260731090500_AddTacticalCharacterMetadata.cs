using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260731090500_AddTacticalCharacterMetadata")]
public partial class AddTacticalCharacterMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE run_character_stat_snapshots
            ADD COLUMN IF NOT EXISTS movement integer NOT NULL DEFAULT 4;

            ALTER TABLE run_character_skill_snapshots
            ADD COLUMN IF NOT EXISTS temporary_slot character varying(16) NOT NULL DEFAULT 'Permanent';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE run_character_stat_snapshots
            DROP COLUMN IF EXISTS movement;

            ALTER TABLE run_character_skill_snapshots
            DROP COLUMN IF EXISTS temporary_slot;
            """);
    }
}
