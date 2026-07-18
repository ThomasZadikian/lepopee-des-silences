using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardTemplateOptionItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "item_type",
                table: "catalog_reward_template_options",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_rarity",
                table: "catalog_reward_template_options",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_effect_type",
                table: "catalog_reward_template_options",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "item_effect_type",
                table: "catalog_reward_template_options");

            migrationBuilder.DropColumn(
                name: "item_rarity",
                table: "catalog_reward_template_options");

            migrationBuilder.DropColumn(
                name: "item_type",
                table: "catalog_reward_template_options");
        }
    }
}
