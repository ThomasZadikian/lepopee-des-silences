using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomShapeAndNodeDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "grid_floor_cells_csv",
                table: "run_rooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "contact_behavior",
                table: "run_nodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "danger_tell",
                table: "run_nodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hidden_state",
                table: "run_nodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grid_floor_cells_csv",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "contact_behavior",
                table: "run_nodes");

            migrationBuilder.DropColumn(
                name: "danger_tell",
                table: "run_nodes");

            migrationBuilder.DropColumn(
                name: "hidden_state",
                table: "run_nodes");
        }
    }
}
