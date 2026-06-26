using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillDefinitionEffectFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "effect_duration_ticks",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "effect_kind",
                table: "catalog_skill_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "effect_magnitude",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "effect_stat",
                table: "catalog_skill_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "effect_status_key",
                table: "catalog_skill_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "effect_tick_interval",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "effect_duration_ticks",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_kind",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_magnitude",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_stat",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_status_key",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_tick_interval",
                table: "catalog_skill_definitions");
        }
    }
}
