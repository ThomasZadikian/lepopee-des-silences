using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistRewardOffersAndCatalogItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "run_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "definition_version",
                table: "run_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "effect_set_key",
                table: "run_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "effect_summary",
                table: "run_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_usable_in_combat",
                table: "run_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_usable_outside_combat",
                table: "run_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle",
                table: "run_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_stack",
                table: "run_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "narrative_text",
                table: "run_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_reward_option_id",
                table: "run_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usage_mode",
                table: "run_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "run_reward_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    combat_tier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    difficulty_multiplier = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    reward_power_multiplier = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    catalog_reward_template_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    catalog_reward_template_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    selected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_reward_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_reward_offers_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_reward_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    payload_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    payload_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    effect_set_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    effect_set_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    base_amount = table.Column<int>(type: "integer", nullable: true),
                    scaled_amount = table.Column<int>(type: "integer", nullable: true),
                    is_selected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    selection_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_reward_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_reward_options_run_reward_offers_reward_offer_id",
                        column: x => x.reward_offer_id,
                        principalTable: "run_reward_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_items_definition_key",
                table: "run_items",
                column: "definition_key");

            migrationBuilder.CreateIndex(
                name: "IX_run_items_source_reward_option_id",
                table: "run_items",
                column: "source_reward_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_reward_offers_run_id",
                table: "run_reward_offers",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_reward_offers_state",
                table: "run_reward_offers",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_run_reward_options_reward_offer_id",
                table: "run_reward_options",
                column: "reward_offer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_reward_options");

            migrationBuilder.DropTable(
                name: "run_reward_offers");

            migrationBuilder.DropIndex(
                name: "IX_run_items_definition_key",
                table: "run_items");

            migrationBuilder.DropIndex(
                name: "IX_run_items_source_reward_option_id",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "category",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "definition_version",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "effect_set_key",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "effect_summary",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "is_usable_in_combat",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "is_usable_outside_combat",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "lifecycle",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "max_stack",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "narrative_text",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "source_reward_option_id",
                table: "run_items");

            migrationBuilder.DropColumn(
                name: "usage_mode",
                table: "run_items");
        }
    }
}
