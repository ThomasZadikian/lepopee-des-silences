using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalRuleStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "run_room_local_rule_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_rule_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    cumulative_severity = table.Column<int>(type: "integer", nullable: false),
                    has_been_informed = table.Column<bool>(type: "boolean", nullable: false),
                    triggered_thresholds_csv = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_room_local_rule_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_room_local_rule_states_run_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "run_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_room_local_rule_states_room_id",
                table: "run_room_local_rule_states",
                column: "room_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_room_local_rule_states");
        }
    }
}
