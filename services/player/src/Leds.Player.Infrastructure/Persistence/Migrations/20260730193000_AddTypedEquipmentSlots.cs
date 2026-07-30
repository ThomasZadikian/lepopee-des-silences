using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

public partial class AddTypedEquipmentSlots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "equipment_slot",
            table: "player_character_items",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Relic");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "equipment_slot",
            table: "player_character_items");
    }
}
