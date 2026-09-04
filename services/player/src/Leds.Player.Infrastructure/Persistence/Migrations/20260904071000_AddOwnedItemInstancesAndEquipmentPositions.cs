using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlayerDbContext))]
[Migration("20260904071000_AddOwnedItemInstancesAndEquipmentPositions")]
public partial class AddOwnedItemInstancesAndEquipmentPositions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_player_character_items_player_character_id_item_definition_key", "player_character_items");
        migrationBuilder.DropIndex("IX_player_permanent_items_player_profile_id_item_definition_key", "player_permanent_items");
        migrationBuilder.DropColumn("is_equipped", "player_character_items");
        migrationBuilder.RenameColumn("equipment_slot", "player_character_items", "equipment_position");
        // The feature explicitly resets the incompatible legacy loadout model. Old rows used
        // definition-key identity and up to three anonymous relic positions, which cannot be
        // migrated without inventing instance/position semantics.
        migrationBuilder.Sql("DELETE FROM player_character_items;");
        migrationBuilder.AlterColumn<string>(
            "equipment_position", "player_character_items", "character varying(32)", maxLength: 32,
            nullable: true, oldClrType: typeof(string), oldType: "character varying(16)", oldMaxLength: 16);
        migrationBuilder.CreateIndex(
            "IX_player_character_items_player_character_id_equipment_position",
            "player_character_items", new[] { "player_character_id", "equipment_position" },
            unique: true, filter: "equipment_position IS NOT NULL");
        migrationBuilder.CreateIndex(
            "IX_player_permanent_items_player_profile_id_item_definition_key",
            "player_permanent_items", new[] { "player_profile_id", "item_definition_key" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_player_character_items_player_character_id_equipment_position", "player_character_items");
        migrationBuilder.DropIndex("IX_player_permanent_items_player_profile_id_item_definition_key", "player_permanent_items");
        migrationBuilder.RenameColumn("equipment_position", "player_character_items", "equipment_slot");
        migrationBuilder.AlterColumn<string>(
            "equipment_slot", "player_character_items", "character varying(16)", maxLength: 16,
            nullable: false, defaultValue: "Relic", oldClrType: typeof(string), oldType: "character varying(32)", oldMaxLength: 32, oldNullable: true);
        migrationBuilder.AddColumn<bool>("is_equipped", "player_character_items", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.CreateIndex(
            "IX_player_character_items_player_character_id_item_definition_key",
            "player_character_items", new[] { "player_character_id", "item_definition_key" }, unique: true);
        migrationBuilder.CreateIndex(
            "IX_player_permanent_items_player_profile_id_item_definition_key",
            "player_permanent_items", new[] { "player_profile_id", "item_definition_key" }, unique: true);
    }
}
