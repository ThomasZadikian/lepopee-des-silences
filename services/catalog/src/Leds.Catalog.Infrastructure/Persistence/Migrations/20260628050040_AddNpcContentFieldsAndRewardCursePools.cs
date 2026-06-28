using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcContentFieldsAndRewardCursePools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dialogue_graph_json",
                table: "catalog_npc_definitions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emotional_affinity",
                table: "catalog_npc_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Neutral");

            migrationBuilder.AddColumn<string>(
                name: "encounter_keys_json",
                table: "catalog_npc_definitions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "is_recurring",
                table: "catalog_npc_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "persona_json",
                table: "catalog_npc_definitions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wounds_json",
                table: "catalog_npc_definitions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "catalog_reward_curse_pools",
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
                    table.PrimaryKey("PK_catalog_reward_curse_pools", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_curse_pools_key",
                table: "catalog_reward_curse_pools",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reward_curse_pools_status",
                table: "catalog_reward_curse_pools",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_reward_curse_pools");

            migrationBuilder.DropColumn(
                name: "dialogue_graph_json",
                table: "catalog_npc_definitions");

            migrationBuilder.DropColumn(
                name: "emotional_affinity",
                table: "catalog_npc_definitions");

            migrationBuilder.DropColumn(
                name: "encounter_keys_json",
                table: "catalog_npc_definitions");

            migrationBuilder.DropColumn(
                name: "is_recurring",
                table: "catalog_npc_definitions");

            migrationBuilder.DropColumn(
                name: "persona_json",
                table: "catalog_npc_definitions");

            migrationBuilder.DropColumn(
                name: "wounds_json",
                table: "catalog_npc_definitions");
        }
    }
}
