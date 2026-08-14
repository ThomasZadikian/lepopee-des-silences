using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomGridSurfaceAndDecorPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "grid_decor_placements_csv",
                table: "run_rooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "grid_surface_overrides_csv",
                table: "run_rooms",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grid_decor_placements_csv",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_surface_overrides_csv",
                table: "run_rooms");
        }
    }
}
