using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnemyBestiaireFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "magic_attack",
                table: "catalog_enemy_stat_blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "magic_defense",
                table: "catalog_enemy_stat_blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "rarity",
                table: "catalog_enemy_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Common");

            migrationBuilder.AddColumn<string>(
                name: "registre",
                table: "catalog_enemy_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "menace_level",
                table: "catalog_enemy_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "bound_room_keys_json",
                table: "catalog_enemy_definitions",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bound_room_keys_json",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "menace_level",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "registre",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "rarity",
                table: "catalog_enemy_definitions");

            migrationBuilder.DropColumn(
                name: "magic_defense",
                table: "catalog_enemy_stat_blocks");

            migrationBuilder.DropColumn(
                name: "magic_attack",
                table: "catalog_enemy_stat_blocks");
        }
    }
}
