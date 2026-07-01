using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnemyLootTablesAndGenericLootPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_enemy_loot_tables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    enemy_definition_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entries_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_enemy_loot_tables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_generic_loot_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entries_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_generic_loot_pools", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_loot_tables_enemy_definition_key",
                table: "catalog_enemy_loot_tables",
                column: "enemy_definition_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_loot_tables_key",
                table: "catalog_enemy_loot_tables",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_enemy_loot_tables_status",
                table: "catalog_enemy_loot_tables",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_generic_loot_pools_key",
                table: "catalog_generic_loot_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_generic_loot_pools_status",
                table: "catalog_generic_loot_pools",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_enemy_loot_tables");

            migrationBuilder.DropTable(
                name: "catalog_generic_loot_pools");
        }
    }
}
