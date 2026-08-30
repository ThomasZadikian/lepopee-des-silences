using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameItemTypeToFlavorTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "item_type",
                table: "catalog_item_definitions",
                newName: "flavor_tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "flavor_tag",
                table: "catalog_item_definitions",
                newName: "item_type");
        }
    }
}
