using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPalaceRoomStateToRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "palace_state",
                table: "run_rooms",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Neutral");

            migrationBuilder.CreateIndex(
                name: "IX_run_rooms_palace_state",
                table: "run_rooms",
                column: "palace_state");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_run_rooms_palace_state",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "palace_state",
                table: "run_rooms");
        }
    }
}
