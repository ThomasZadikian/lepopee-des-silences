using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSkillEffectFieldsWithEffectsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "effects_json",
                table: "catalog_skill_definitions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "effect_kind",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_status_key",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_magnitude",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_duration_ticks",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_tick_interval",
                table: "catalog_skill_definitions");

            migrationBuilder.DropColumn(
                name: "effect_stat",
                table: "catalog_skill_definitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "effects_json",
                table: "catalog_skill_definitions");

            migrationBuilder.AddColumn<string>(
                name: "effect_kind",
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
                name: "effect_magnitude",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "effect_duration_ticks",
                table: "catalog_skill_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "effect_tick_interval",
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
        }
    }
}
