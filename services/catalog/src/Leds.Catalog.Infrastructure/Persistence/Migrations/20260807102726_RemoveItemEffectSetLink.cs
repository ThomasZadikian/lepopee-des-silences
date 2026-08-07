using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveItemEffectSetLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_item_definitions_catalog_effect_sets_effect_set_id",
                table: "catalog_item_definitions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_item_definitions_effect_set_id",
                table: "catalog_item_definitions");

            migrationBuilder.DropColumn(
                name: "effect_set_id",
                table: "catalog_item_definitions");

            migrationBuilder.AlterColumn<int>(
                name: "effect_value",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: false,
                comment: "Sole source of truth for a used item's effect magnitude, alongside effect_run_type. See ItemDefinitionEntity.EffectRunType.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Legacy compatibility column. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.");

            migrationBuilder.AlterColumn<string>(
                name: "duration",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Legacy compatibility column. Use lifecycle/usage_mode for data-model-0.1 definitions.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Legacy compatibility column. Use lifecycle/usage_mode/effect_set_id for data-model-0.1 definitions.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "effect_value",
                table: "catalog_item_definitions",
                type: "integer",
                nullable: false,
                comment: "Legacy compatibility column. Canonical effects live in catalog_effect_sets/catalog_effect_definitions.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Sole source of truth for a used item's effect magnitude, alongside effect_run_type. See ItemDefinitionEntity.EffectRunType.");

            migrationBuilder.AlterColumn<string>(
                name: "duration",
                table: "catalog_item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Legacy compatibility column. Use lifecycle/usage_mode/effect_set_id for data-model-0.1 definitions.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Legacy compatibility column. Use lifecycle/usage_mode for data-model-0.1 definitions.");

            migrationBuilder.AddColumn<Guid>(
                name: "effect_set_id",
                table: "catalog_item_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_effect_set_id",
                table: "catalog_item_definitions",
                column: "effect_set_id");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_item_definitions_catalog_effect_sets_effect_set_id",
                table: "catalog_item_definitions",
                column: "effect_set_id",
                principalTable: "catalog_effect_sets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
