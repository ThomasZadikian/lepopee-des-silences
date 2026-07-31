using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260730220000_AddRunEquipmentSnapshot")]
public partial class AddRunEquipmentSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "equipped_item_keys_csv",
            table: "run_character_snapshots",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "equipped_item_keys_csv",
            table: "run_character_snapshots");
    }
}
