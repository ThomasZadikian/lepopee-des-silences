using Leds.Player.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlayerDbContext))]
[Migration("20260730230000_RemoveRecoveryStat")]
public partial class RemoveRecoveryStat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("recovery", "player_character_stat_blocks");

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<int>(
            "recovery", "player_character_stat_blocks", nullable: false, defaultValue: 5);
}
