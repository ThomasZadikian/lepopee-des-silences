using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPalaceLawPromulgationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rarity",
                table: "catalog_palace_law_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Commun");

            migrationBuilder.AddColumn<string>(
                name: "polarity",
                table: "catalog_palace_law_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Neutre");

            migrationBuilder.AddColumn<bool>(
                name: "is_majeure",
                table: "catalog_palace_law_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "room_key",
                table: "catalog_palace_law_definitions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_cumul_exempt",
                table: "catalog_palace_law_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "exclusion_keys_json",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rarity",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "polarity",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "is_majeure",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "room_key",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "is_cumul_exempt",
                table: "catalog_palace_law_definitions");

            migrationBuilder.DropColumn(
                name: "exclusion_keys_json",
                table: "catalog_palace_law_definitions");
        }
    }
}
