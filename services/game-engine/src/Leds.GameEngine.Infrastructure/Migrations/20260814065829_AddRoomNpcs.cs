using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomNpcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_room_npcs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_npc_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    origin_x = table.Column<int>(type: "integer", nullable: false),
                    origin_y = table.Column<int>(type: "integer", nullable: false),
                    x = table.Column<int>(type: "integer", nullable: false),
                    y = table.Column<int>(type: "integer", nullable: false),
                    behavior = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    awareness = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    awareness_radius = table.Column<int>(type: "integer", nullable: false),
                    waypoints_csv = table.Column<string>(type: "text", nullable: false),
                    waypoint_index = table.Column<int>(type: "integer", nullable: false),
                    step_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_room_npcs", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_room_npcs_run_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "run_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_room_npcs_room_id",
                table: "run_room_npcs",
                column: "room_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_room_npcs");
        }
    }
}
