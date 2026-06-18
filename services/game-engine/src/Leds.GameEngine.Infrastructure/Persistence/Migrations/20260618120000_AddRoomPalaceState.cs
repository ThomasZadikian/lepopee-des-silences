using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    public partial class AddRoomPalaceState : Migration
    {
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
                name: "ix_run_rooms_palace_state",
                table: "run_rooms",
                column: "palace_state");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_run_rooms_palace_state",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "palace_state",
                table: "run_rooms");
        }
    }
}
