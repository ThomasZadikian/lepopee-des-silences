using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomReachabilityGraphAndWorlds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "exclude_from_open_pool",
                table: "catalog_room_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "reachability_mode",
                table: "catalog_room_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "triggers_strict_chain",
                table: "catalog_room_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "world_definition_id",
                table: "catalog_room_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalog_room_reachability",
                columns: table => new
                {
                    from_room_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_room_definition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_reachability", x => new { x.from_room_definition_id, x.to_room_definition_id });
                    table.ForeignKey(
                        name: "FK_catalog_room_reachability_catalog_room_definitions_from_roo~",
                        column: x => x.from_room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_room_reachability_catalog_room_definitions_to_room_~",
                        column: x => x.to_room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_room_theme_affinities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme_from = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    theme_to = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_room_theme_affinities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_world_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    entry_room_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_world_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_world_definitions_catalog_room_definitions_entry_ro~",
                        column: x => x.entry_room_definition_id,
                        principalTable: "catalog_room_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_definitions_world_definition_id",
                table: "catalog_room_definitions",
                column: "world_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_reachability_to_room_definition_id",
                table: "catalog_room_reachability",
                column: "to_room_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_room_theme_affinities_theme_from_theme_to",
                table: "catalog_room_theme_affinities",
                columns: new[] { "theme_from", "theme_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_world_definitions_entry_room_definition_id",
                table: "catalog_world_definitions",
                column: "entry_room_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_world_definitions_key",
                table: "catalog_world_definitions",
                column: "key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_room_definitions_catalog_world_definitions_world_de~",
                table: "catalog_room_definitions",
                column: "world_definition_id",
                principalTable: "catalog_world_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_room_definitions_catalog_world_definitions_world_de~",
                table: "catalog_room_definitions");

            migrationBuilder.DropTable(
                name: "catalog_room_reachability");

            migrationBuilder.DropTable(
                name: "catalog_room_theme_affinities");

            migrationBuilder.DropTable(
                name: "catalog_world_definitions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_room_definitions_world_definition_id",
                table: "catalog_room_definitions");

            migrationBuilder.DropColumn(
                name: "exclude_from_open_pool",
                table: "catalog_room_definitions");

            migrationBuilder.DropColumn(
                name: "reachability_mode",
                table: "catalog_room_definitions");

            migrationBuilder.DropColumn(
                name: "triggers_strict_chain",
                table: "catalog_room_definitions");

            migrationBuilder.DropColumn(
                name: "world_definition_id",
                table: "catalog_room_definitions");
        }
    }
}
