using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemContainerAndLiquid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_container",
                table: "catalog_item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "container_capacity",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_liquid",
                table: "catalog_item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_container",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "container_capacity",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "is_liquid",
                table: "catalog_item_definitions");
        }
    }
}
