using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillDefinitionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "catalog_skill_definitions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Physical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "catalog_skill_definitions");
        }
    }
}
