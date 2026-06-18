using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcDefinitionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_npc_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    min_depth = table.Column<int>(type: "integer", nullable: true),
                    max_depth = table.Column<int>(type: "integer", nullable: true),
                    compatible_room_types_json = table.Column<string>(type: "text", nullable: false),
                    compatible_palace_room_states_json = table.Column<string>(type: "text", nullable: false),
                    compatible_room_climates_json = table.Column<string>(type: "text", nullable: false),
                    tags_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_npc_definitions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_npc_definitions_key",
                table: "catalog_npc_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_npc_definitions_status",
                table: "catalog_npc_definitions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_npc_definitions");
        }
    }
}
