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
        => migrationBuilder.AddColumn<int>(
            "movement",
            "player_character_stat_blocks",
            nullable: false,
            defaultValue: 4);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("movement", "player_character_stat_blocks");
}
