using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncGameEngineModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "exclusion_keys_json",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "exclusion_keys_json",
                table: "catalog_palace_law_definitions",
                type: "text",
                nullable: true,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "[]");
        }
    }
}
