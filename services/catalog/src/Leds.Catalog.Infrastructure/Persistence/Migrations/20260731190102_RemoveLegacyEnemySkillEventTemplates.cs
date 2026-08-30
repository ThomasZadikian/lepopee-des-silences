using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyEnemySkillEventTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_enemy_templates");

            migrationBuilder.DropTable(
                name: "catalog_event_templates");

            migrationBuilder.DropTable(
                name: "catalog_skill_templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_enemy_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    archetype = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    element = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    experience_reward = table.Column<int>(type: "integer", nullable: false),
                    gold_reward = table.Column<int>(type: "integer", nullable: false),
                    intelligence = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    magical_resistance = table.Column<int>(type: "integer", nullable: false),
                    max_health = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    physical_resistance = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_event_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    default_outcome_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    max_risk_level = table.Column<int>(type: "integer", nullable: false),
                    min_risk_level = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    narrative_tags_json = table.Column<string>(type: "text", nullable: false),
                    requires_player_choice = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_event_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_skill_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    element = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    heal_power = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_skill_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_templates_key",
                table: "catalog_enemy_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_templates_status",
                table: "catalog_enemy_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_event_templates_key",
                table: "catalog_event_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_event_templates_status",
                table: "catalog_event_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_templates_key",
                table: "catalog_skill_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_templates_status",
                table: "catalog_skill_templates",
                column: "status");
        }
    }
}
