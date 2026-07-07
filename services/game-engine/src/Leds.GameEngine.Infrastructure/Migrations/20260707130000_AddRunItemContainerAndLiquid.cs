using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunItemContainerAndLiquid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_container",
                table: "run_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "container_capacity",
                table: "run_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_liquid",
                table: "run_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "contained_liquid_definition_key",
                table: "run_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_container",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "container_capacity",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "is_liquid",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "contained_liquid_definition_key",
                table: "run_items");
        }
    }
}
