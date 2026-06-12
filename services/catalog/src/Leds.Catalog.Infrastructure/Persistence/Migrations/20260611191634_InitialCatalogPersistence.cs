using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_enemy_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    archetype = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    base_difficulty = table.Column<int>(type: "integer", nullable: false),
                    min_risk_level = table.Column<int>(type: "integer", nullable: false),
                    max_risk_level = table.Column<int>(type: "integer", nullable: false),
                    compatible_room_types_json = table.Column<string>(type: "text", nullable: false),
                    tags_json = table.Column<string>(type: "text", nullable: false),
                    skill_keys_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_item_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rarity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    duration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_value = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_item_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_palace_law_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    impact_domains_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_palace_law_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_seed_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seed_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_seed_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_skill_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    effect_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mana_cost = table.Column<int>(type: "integer", nullable: false),
                    charge_cost = table.Column<int>(type: "integer", nullable: false),
                    base_power = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_skill_definitions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_definitions_key",
                table: "catalog_enemy_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_definitions_status",
                table: "catalog_enemy_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_key",
                table: "catalog_item_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_definitions_status",
                table: "catalog_item_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_palace_law_definitions_key",
                table: "catalog_palace_law_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_palace_law_definitions_status",
                table: "catalog_palace_law_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_seed_versions_seed_key_version",
                table: "catalog_seed_versions",
                columns: new[] { "seed_key", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_definitions_key",
                table: "catalog_skill_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_skill_definitions_status",
                table: "catalog_skill_definitions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_enemy_definitions");

            migrationBuilder.DropTable(
                name: "catalog_item_definitions");

            migrationBuilder.DropTable(
                name: "catalog_palace_law_definitions");

            migrationBuilder.DropTable(
                name: "catalog_seed_versions");

            migrationBuilder.DropTable(
                name: "catalog_skill_definitions");
        }
    }
}
