using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260731090500_AddRunCharacterMovement")]
public partial class AddRunCharacterMovement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<int>(
            "movement",
            "run_character_stat_snapshots",
            nullable: false,
            defaultValue: 4);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("movement", "run_character_stat_snapshots");
}
