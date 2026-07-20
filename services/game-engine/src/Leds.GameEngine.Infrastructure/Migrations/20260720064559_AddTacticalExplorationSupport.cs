using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTacticalExplorationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExplorationMode",
                table: "runs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "grid_height",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_movement_budget",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_movement_budget_remaining",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_party_x",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_party_y",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grid_revealed_cells_csv",
                table: "run_rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grid_revealed_node_ids_csv",
                table: "run_rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_start_x",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_start_y",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "grid_width",
                table: "run_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "combat_risk_tier",
                table: "run_nodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExplorationMode",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "grid_height",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_movement_budget",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_movement_budget_remaining",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_party_x",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_party_y",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_revealed_cells_csv",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_revealed_node_ids_csv",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_start_x",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_start_y",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "grid_width",
                table: "run_rooms");

            migrationBuilder.DropColumn(
                name: "combat_risk_tier",
                table: "run_nodes");
        }
    }
}
