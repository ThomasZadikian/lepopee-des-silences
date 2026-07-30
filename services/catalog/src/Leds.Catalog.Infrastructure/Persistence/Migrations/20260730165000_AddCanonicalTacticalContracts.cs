using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations;

public partial class AddCanonicalTacticalContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("movement", "catalog_enemy_stat_blocks", nullable: false, defaultValue: 4);

        migrationBuilder.AddColumn<int>("tactical_range", "catalog_skill_definitions", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>("tactical_area_shape", "catalog_skill_definitions", maxLength: 16, nullable: false, defaultValue: "Single");
        migrationBuilder.AddColumn<bool>("requires_line_of_sight", "catalog_skill_definitions", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>("is_ultimate", "catalog_skill_definitions", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("emotional_register", "catalog_skill_definitions", maxLength: 32, nullable: false, defaultValue: "Neutral");

        migrationBuilder.AddColumn<int>("tactical_range", "catalog_item_definitions", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>("tactical_area_shape", "catalog_item_definitions", maxLength: 16, nullable: false, defaultValue: "Single");
        migrationBuilder.AddColumn<bool>("requires_line_of_sight", "catalog_item_definitions", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("basic_attack_power", "catalog_item_definitions", nullable: true);
        migrationBuilder.AddColumn<string>("basic_attack_category", "catalog_item_definitions", maxLength: 16, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("movement", "catalog_enemy_stat_blocks");

        migrationBuilder.DropColumn("tactical_range", "catalog_skill_definitions");
        migrationBuilder.DropColumn("tactical_area_shape", "catalog_skill_definitions");
        migrationBuilder.DropColumn("requires_line_of_sight", "catalog_skill_definitions");
        migrationBuilder.DropColumn("is_ultimate", "catalog_skill_definitions");
        migrationBuilder.DropColumn("emotional_register", "catalog_skill_definitions");

        migrationBuilder.DropColumn("tactical_range", "catalog_item_definitions");
        migrationBuilder.DropColumn("tactical_area_shape", "catalog_item_definitions");
        migrationBuilder.DropColumn("requires_line_of_sight", "catalog_item_definitions");
        migrationBuilder.DropColumn("basic_attack_power", "catalog_item_definitions");
        migrationBuilder.DropColumn("basic_attack_category", "catalog_item_definitions");
    }
}
