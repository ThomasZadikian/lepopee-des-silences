using Leds.Player.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlayerDbContext))]
[Migration("20260731090000_AddMovementStat")]
public partial class AddMovementStat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql(
            """
            ALTER TABLE player_character_stat_blocks
            ADD COLUMN IF NOT EXISTS movement integer NOT NULL DEFAULT 4;
            """);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql(
            """
            ALTER TABLE player_character_stat_blocks
            DROP COLUMN IF EXISTS movement;
            """);
}
